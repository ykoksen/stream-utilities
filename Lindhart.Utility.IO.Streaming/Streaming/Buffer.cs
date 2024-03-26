using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lindhart.Utility.IO.Streaming
{
    /// <summary>
    /// Buffer. Will not allow reading before it has been written. Will not allow writing before the previously written bytes have been read.
    /// </summary>
    internal class Buffer
    {
        private readonly byte[] _bufferBytes;
        private readonly int _bufferSize;

        private readonly MemoryStream _bufferStream;

        private readonly SemaphoreSlim _readBuffer;

        private readonly SemaphoreSlim _writeBuffer;

        private bool _readingInProgress;

        public bool EndOfStream { get; private set; }

        public Buffer(int bufferSize)
        {
            _bufferSize = bufferSize;
            _bufferBytes = new byte[bufferSize];
            _bufferStream = new MemoryStream(_bufferBytes);

            // We cannot read in the start, since the buffers are not written yet.
            _readBuffer = new SemaphoreSlim(0, 1);

            // We can write in the start.
            _writeBuffer = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Reads from the buffer. Will wait if the buffer has not yet been written to.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken token)
        {
            // Check if we are already in the process of reading
            if (!_readingInProgress)
            {
                // Wait until write is complete
                await _readBuffer.WaitAsync(token);
                _readingInProgress = true;
            }

            int readCount = await _bufferStream.ReadAsync(buffer, token);

            if (readCount == 0)
            {
                // We have read the entire buffer and thus are no longer in the process of reading this. Therefore writing is also allowed.
                _readingInProgress = false;
                _writeBuffer.Release();
            }

            return readCount;
        }

        /// <summary>
        /// Fills the buffer with bytes from the stream or until the stream is empty. If the buffer already contains bytes, it will wait until those bytes have been read.
        /// </summary>
        /// <param name="readFrom"></param>
        /// <returns></returns>
        internal async Task WriteBuffer(Stream readFrom, CancellationToken token)
        {
            // Wait until read is complete
            await _writeBuffer.WaitAsync(token);

            int readCount = 0;
            int count;

            do
            {
                count = await readFrom.ReadAsync(_bufferBytes, readCount, _bufferSize - readCount, token);
                readCount += count;
            }
            while (readCount < _bufferSize && count != 0);

            _bufferStream.SetLength(readCount);
            _bufferStream.Position = 0;
            EndOfStream = count == 0;

            // Allow reading of the buffer
            _readBuffer.Release();            
        }
    }
}
