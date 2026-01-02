using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lindhart.Utility.IO.Streaming
{
    /// <summary>
    /// A class to wrap around an existing <see cref="Stream"/>. This will enable processing content from the buffer, while a background thread is reading new bytes from the underlying <see cref="Stream"/>. 
    /// This is especially useful when reading from a network (hence <see cref="System.Net.Sockets.NetworkStream"/>) while wanting to do another time consuming job example compressing or calculating hash.
    /// </summary>
    public class BufferBackgroundStream : Stream
    {
        private const int DefaultBufferSize = 100_000;

        private readonly Stream _innerStream;
        
        private long _position;

        private MemoryStream _readBuffer;
        private MemoryStream _writeBuffer;

        private Task _writer;

        private readonly CancellationTokenSource _cancellationTokenSource;
        
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _innerStream.Length;

        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override void Close()
        {
            _cancellationTokenSource.Cancel();
            base.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerStream">The <see cref="Stream"/> to wrap and read from.</param>
        /// <param name="bufferSize">The size of the buffer. Note that double this size will be used since there will be a buffer to read from, while another buffer can be used in the background to fetch data in parallel. 
        /// The default buffer size is 100 KB (thereby using 200 KB memory)</param>
        public BufferBackgroundStream(Stream innerStream, int bufferSize = DefaultBufferSize)
        {
            _position = 0;
            _innerStream = innerStream;
            _cancellationTokenSource = new CancellationTokenSource();
            _readBuffer = new MemoryStream(new byte[bufferSize], 0, bufferSize, true, true);
            _writeBuffer = new MemoryStream(new byte[bufferSize], 0, bufferSize, true, true);
            _readBuffer.Position = bufferSize;
            _writer = WriteAsync();
        }

        private async Task Switch()
        {
            // Before we can switch we need to be done writing.
            await _writer;

            // Switch read and write buffers around. We have done reading the read buffer, and we are done writing the write buffer.
            (_readBuffer, _writeBuffer) = (_writeBuffer, _readBuffer);

            // Start writing again in the background
            _writer = WriteAsync();            
        }

        private async Task WriteAsync()
        { 
            var bytes = _writeBuffer.GetBuffer();
            var bufferSize = bytes.Length;

            int readCount = 0;
            int count;

            // Read until the buffer is full, or we've reached the end of the stream.
            do
            {
                count = await _innerStream.ReadAsync(bytes, readCount, bufferSize - readCount, _cancellationTokenSource.Token);
                readCount += count;
            }
            while (readCount < bufferSize && count != 0 && !_cancellationTokenSource.Token.IsCancellationRequested);

            _writeBuffer.SetLength(readCount);
            _writeBuffer.Position = 0;            
        }

        public override void Flush()
        {            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // On purpose the async logic has not been rewritten as sync logic (in order to avoid duplicate code).
            // This is not pretty, but then again why would you use background thread and not run it async?
            return ReadAsync(new Memory<byte>(buffer, offset, count), CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_readBuffer.Position == _readBuffer.Length)
            {
                await Switch();
            }

            int readCount = await _readBuffer.ReadAsync(buffer, cancellationToken);
            _position += readCount;
            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if(_disposed) return;

            if (disposing)
            {
                _cancellationTokenSource.Cancel();

                try
                {
                    _writer.GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    // Do nothing since we're in dispose method.
                }

                _writer.Dispose();

                _innerStream.Dispose();
                _cancellationTokenSource.Dispose();
                _readBuffer.Dispose();
                _writeBuffer.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public sealed override async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await DisposeAsyncCore();

            _disposed = true;
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            _cancellationTokenSource.Cancel();

            try
            {
                await _writer;
            }
            catch (Exception)
            {
                // Do nothing since we're in dispose method.
            }

            _writer.Dispose();

            await _innerStream.DisposeAsync();
            _cancellationTokenSource.Dispose();
            await _readBuffer.DisposeAsync();
            await _writeBuffer.DisposeAsync();
        }
    }
}
