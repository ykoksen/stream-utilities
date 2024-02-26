using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Lindhart.Utility.IO.Streaming
{
    public delegate Stream StreamConstructor(Stream outputStream);

    public sealed class StreamInverter : Stream
    {

        #region constructor / cleanup

        /// <summary>
        /// Use the <see cref="StreamConstructor"/> delegate to instantiate the constructor of the class that does the actual job.
        /// </summary>
        /// <param name="inputStream">The input <see cref="Stream"/> that will be read.</param>
        /// <param name="constructor">The delegate that runs the constructor of the <see cref="Stream"/> that should do the actual work, and where the input and output streams are to be exchanged</param>
        /// <example>
        /// This example uses the <see cref="System.IO.Compression.GZipStream"/> which requires that the output <see cref="Stream"/> is a parameter in the constructor, and that the input is the <see cref="System.IO.Compression.GZipStream"/> itself.
        /// This is reversed so the <see cref="StreamInverter"/> now is the compressed output, and the input is a parameter instead. 
        /// <code>
        /// public Stream GetCompressedStream(Stream input)
        /// {
        ///     return new StreamInverter(input, output => new GZipStream(output, CompressionLevel.Optimal, true));
        /// }
        /// </code>
        /// </example>
        public StreamInverter(Stream inputStream, StreamConstructor constructor)
        {
            try
            {
                _inputStream = inputStream;
                _outputStream = new NonDisposableMemoryStream();
                _workerStream = constructor(_outputStream);
                _inputBuffer = new byte[50_000];
                _memoryBuffer = new Memory<byte>(_inputBuffer);
            }
            catch
            {
                Cleanup();
                throw;
            }
        }

        private void Cleanup()
        {
            _workerStream?.Dispose();
            _outputStream?.Dispose();
            _inputStream?.Dispose();
        }

        #endregion

        #region private variables

        private bool EndOfInputStreamReached = false;

        private readonly Stream _inputStream;
        private readonly NonDisposableMemoryStream _outputStream;
        private readonly Stream _workerStream;
        private readonly byte[] _inputBuffer;
        private readonly Memory<byte> _memoryBuffer;

        #endregion

        #region stream overrides

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => 0;

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            // We might write several times to the _gzipStream without _outputStream receiveving anything.
            // As soon as anything is written the length is higher than the position, and only reading can be done until the end of the _outputStream.
            while ((_outputStream.Position >= _outputStream.Length) && !EndOfInputStreamReached)
            {
                // No unread data available in the output buffer
                // -> release memory of output buffer and read new data from the source and feed through the pipeline
                _outputStream.SetLength(0);
                
                var readCount = _inputStream.Read(_inputBuffer, 0, _inputBuffer.Length);
                if (readCount == 0)
                {
                    EndOfInputStreamReached = true;
                    _workerStream.Flush();
                    _workerStream.Dispose(); // because Flush() might not actually flush (ex. on DeflateStream, where only Dispose actually flushes)
                }
                else
                {

                    _workerStream.Write(_inputBuffer, 0, readCount);
                }
                _outputStream.Position = 0;
            }

            return _outputStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token = default)
        {
            // We might write several times to the _gzipStream without _outputStream receiveving anything.
            // As soon as anything is written the length is higher than the position, and only reading can be done until the end of the _outputStream.
            while ((_outputStream.Position >= _outputStream.Length) && !EndOfInputStreamReached)
            {
                // No unread data available in the output buffer
                // -> release memory of output buffer and read new data from the source and feed through the pipeline
                _outputStream.SetLength(0);

                var readCount = await _inputStream.ReadAsync(_inputBuffer, 0, _inputBuffer.Length, token);
                if (readCount == 0)
                {
                    EndOfInputStreamReached = true;
                    _workerStream.Flush();
                    _workerStream.Dispose(); // because Flush() does not actually flush...
                }
                else
                {

                    await _workerStream.WriteAsync(_inputBuffer, 0, readCount, token);
                }
                _outputStream.Position = 0;
            }

            return await _outputStream.ReadAsync(buffer, offset, count, token);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            // We might write several times to the _gzipStream without _outputStream receiveving anything.
            // As soon as anything is written the length is higher than the position, and only reading can be done until the end of the _outputStream.
            while ((_outputStream.Position >= _outputStream.Length) && !EndOfInputStreamReached)
            {
                // No unread data available in the output buffer
                // -> release memory of output buffer and read new data from the source and feed through the pipeline
                _outputStream.SetLength(0);
                var readCount = await _inputStream.ReadAsync(_memoryBuffer, token);
                if (readCount == 0)
                {
                    EndOfInputStreamReached = true;
                    _workerStream.Flush();
                    _workerStream.Dispose(); // because Flush() does not actually flush...
                }
                else
                {

                    await _workerStream.WriteAsync(_memoryBuffer[..readCount], token);
                }
                _outputStream.Position = 0;
            }

            return await _outputStream.ReadAsync(buffer, token);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                Cleanup();
        }

        #endregion

        private class NonDisposableMemoryStream : MemoryStream
        {
            public NonDisposableMemoryStream() :base()
            { }

            protected override void Dispose(bool disposing)
            {
                // Do nothing. This is to avoid classes outside our control to dispose it.
            }

            public override ValueTask DisposeAsync()
            {
                // Do nothing. This is to avoid classes outside our control to dispose it.
                return new ValueTask(Task.CompletedTask);
            }

            internal ValueTask DisposeInternAsync()
            {
                return base.DisposeAsync();
            }
        }
    }

    
}
