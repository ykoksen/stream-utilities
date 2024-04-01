using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Lindhart.Utility.IO.Streaming
{
    public delegate Stream StreamConstructor(Stream outputStream);

    /// <summary>
    /// A class for inverting the output and input of a <see cref="Stream"/> that works on another <see cref="Stream"/>, example <see cref="GZipStream"/>. This class only supports reading. 
    /// </summary>
    public sealed partial class StreamInverter : Stream
    {
        private const int DefaultBufferSize = 81920;

        #region constructor / cleanup

        /// <summary>
        /// Use the <see cref="StreamConstructor"/> delegate to instantiate the constructor of the class that does the actual job.
        /// </summary>
        /// <param name="inputStream">The input <see cref="Stream"/> that will be read.</param>
        /// <param name="constructor">The delegate that runs the constructor of the <see cref="Stream"/> that should do the actual work, and where the input and output streams are to be exchanged</param>
        /// <param name="bufferSize">The maximum size of the 2 buffers used internally. Normally the default value should just be used. The default value is 81920.</param>
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
        public StreamInverter(Stream inputStream, StreamConstructor constructor, int bufferSize = DefaultBufferSize)
        {
            _inputStream = inputStream;
            _writeLock = new SemaphoreSlim(0, 1);
            _outputStream = new DelegatingWriteLockStream(new NonDisposableMemoryStream(), _writeLock);
            _workerStream = constructor(_outputStream);
            _inputBuffer = new byte[bufferSize];
            _memoryBuffer = new Memory<byte>(_inputBuffer);
        }

        #endregion

        #region private variables

        private bool EndOfInputStreamReached = false;

        private readonly Stream _inputStream;
        private readonly DelegatingWriteLockStream _outputStream;
        private readonly Stream _workerStream;
        private readonly byte[] _inputBuffer;
        private readonly Memory<byte> _memoryBuffer;

        /// <summary>
        /// This writelock is used in the temporary output Stream, so we ensure that we either write to it or read from it - not both at the same time.
        /// </summary>
        private readonly SemaphoreSlim _writeLock;

        #endregion

        #region stream overrides

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

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
                    // Allow to write to _outputStream - we are at the end anyways, and on flush and dispose everything should be written.
                    _writeLock.Release();
                    EndOfInputStreamReached = true;
                    _workerStream.Flush();
                    _workerStream.Dispose(); // because Flush() might not actually flush (ex. on DeflateStream, where only Dispose actually flushes)
                }
                else
                {
                    // During these lines we allow to write to _outputStream
                    _writeLock.Release();
                    _workerStream.Write(_inputBuffer, 0, readCount);
                    _writeLock.Wait();
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
                    // Allow to write to _outputStream - we are at the end anyways, and on flush and dispose everything should be written.
                    _writeLock.Release();
                    EndOfInputStreamReached = true;
                    await _workerStream.FlushAsync();
                    await _workerStream.DisposeAsync(); // because Flush() might not actually flush(ex.on DeflateStream, where only Dispose actually flushes)
                }
                else
                {
                    // During these lines we allow to write to _outputStream
                    _writeLock.Release();
                    await _workerStream.WriteAsync(_inputBuffer, 0, readCount, token);
                    await _writeLock.WaitAsync();
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
                    // Allow to write to _outputStream - we are at the end anyways, and on flush and dispose everything should be written.
                    _writeLock.Release();
                    EndOfInputStreamReached = true;
                    await _workerStream.FlushAsync();
                    await _workerStream.DisposeAsync(); // because Flush() might not actually flush(ex.on DeflateStream, where only Dispose actually flushes)
                }
                else
                {
                    // During these lines we allow to write to _outputStream
                    _writeLock.Release();
                    await _workerStream.WriteAsync(_memoryBuffer[..readCount], token);
                    await _writeLock.WaitAsync();
                }

                _outputStream.Position = 0;
            }

            return await _outputStream.ReadAsync(buffer, token);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _workerStream.Dispose();
                _outputStream.Dispose();
                _inputStream.Dispose();
            }
        }

        public override async ValueTask DisposeAsync()
        {
            // Perform async cleanup.
            await DisposeAsyncCore();

            // Dispose of unmanaged resources.
            Dispose(false);

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        private async ValueTask DisposeAsyncCore()
        {
            await _workerStream.DisposeAsync();
            await _outputStream.DisposeAsync();
            await _inputStream.DisposeAsync();
        }

        #endregion

        private class NonDisposableMemoryStream : MemoryStream
        {
            public NonDisposableMemoryStream() : base()
            {
            }

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
