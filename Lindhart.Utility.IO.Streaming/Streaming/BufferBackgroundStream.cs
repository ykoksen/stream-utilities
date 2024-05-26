using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lindhart.Utility.IO.Streaming
{
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

        public override long Position { get => _position; set => throw new System.NotSupportedException(); }

        public override void Close()
        {
            _cancellationTokenSource.Cancel();
            base.Close();
        }

        public BufferBackgroundStream(Stream innerStream, int bufferSize = DefaultBufferSize)
        {
            _position = 0;
            _innerStream = innerStream;
            _cancellationTokenSource = new CancellationTokenSource();
            _readBuffer = new MemoryStream(new byte[bufferSize], 0, bufferSize, true, true);
            _writeBuffer = new MemoryStream(new byte[bufferSize], 0, bufferSize, true, true);
            _readBuffer.Position = bufferSize;
            _writer = Write();
        }

        public async Task Switch()
        {
            // Before we can switch we need to be done writing.
            await _writer;

            // Switch read and write buffers around. We have done reading the read buffer and we are done writing the write buffer.
            (_readBuffer, _writeBuffer) = (_writeBuffer, _readBuffer);

            // Start writing again in the background
            _writer = Write();            
        }

        public async Task Write()
        { 
            var bytes = _writeBuffer.GetBuffer();
            var bufferSize = bytes.Length;

            int readCount = 0;
            int count;

            // Read until the buffer is full or we've reached the end of the stream.
            do
            {
                count = await _innerStream.ReadAsync(bytes, readCount, bufferSize - readCount, _cancellationTokenSource.Token);
                readCount += count;
            }
            while (readCount < bufferSize && count != 0);

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
            return ReadAsync(new Memory<byte>(buffer, offset, count), CancellationToken.None).Result;
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
            throw new System.NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotSupportedException();
        }
    }
}
