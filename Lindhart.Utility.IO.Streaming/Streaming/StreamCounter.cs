using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lindhart.Utility.IO.Streaming
{
    /// <summary>
    /// A <see cref="Stream"/> wrapper that counts the bytes written or read. This does not dispose the underlying stream when it is disposed.
    /// </summary>
    public class StreamCounter : Stream
    {
        private readonly Stream _stream;

        public StreamCounter(Stream stream)
        {
            _stream = stream;
            BytesRead = 0;
            BytesWritten = 0;
        }

        /// <summary>
        /// Number of bytes read from the underlying Stream. Note that bytes that have been read twice will be counted twice (can happen when changing position)
        /// </summary>
        public long BytesRead { get; private set; }

        /// <summary>
        /// Number of bytes written to the underlying Stream. Note that if bytes have been overwritten it will be counted twice (can happen when changing position)
        /// </summary>
        public long BytesWritten { get; private set; }

        /// <inheritdoc />
        public override bool CanRead => _stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _stream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => _stream.CanWrite;

        /// <inheritdoc />
        public override long Length => _stream.Length;

        /// <inheritdoc />
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        /// <inheritdoc />
        public override void Flush()
        {
            _stream.Flush();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _stream.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            BytesWritten += count;
            _stream.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <inheritdoc />
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            BytesWritten += count;
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult)
        {
            var readCount = _stream.EndRead(asyncResult);
            BytesRead += readCount;
            return readCount;
        }

        /// <inheritdoc />
        public override void EndWrite(IAsyncResult asyncResult)
        {
            _stream.EndWrite(asyncResult);
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var readBytes = await _stream.ReadAsync(buffer, offset, count, cancellationToken);
            BytesRead += readBytes;
            return readBytes;
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            BytesWritten += count;
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            var back = _stream.ReadByte();
            if (back != -1)
                BytesRead++;
            return back;
             
        }
    }
}
