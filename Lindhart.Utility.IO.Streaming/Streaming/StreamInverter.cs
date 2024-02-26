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

        public StreamInverter(Stream inputStream, StreamConstructor constructor)
        {
            try
            {
                _inputStream = inputStream;
                _outputStream = new MemoryStream();
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
        private readonly MemoryStream _outputStream;
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
                    _workerStream.Dispose(); // because Flush() does not actually flush...
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

    }
}
