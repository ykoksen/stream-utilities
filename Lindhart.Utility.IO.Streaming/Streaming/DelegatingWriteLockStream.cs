using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Lindhart.Utility.IO.Streaming
{

    public sealed partial class StreamInverter
    {
        /// <summary>
        /// Delegating stream that will wait with writing to underlying <see cref="Stream"/> to when the lock is available.
        /// </summary>
        private class DelegatingWriteLockStream : Stream
        {
            private readonly Stream _innerStream;
            private readonly SemaphoreSlim _writeLock;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="innerStream">The <see cref="Stream"/> where all calls will be delegated to.</param>
            /// <param name="writeLock">The lock that is obtained before writing, and released afterwards.</param>
            public DelegatingWriteLockStream(Stream innerStream, SemaphoreSlim writeLock)
            {
                _innerStream = innerStream;
                _writeLock = writeLock;
            }

            public override bool CanRead => _innerStream.CanRead;

            public override bool CanSeek => _innerStream.CanSeek;

            public override bool CanWrite => true;

            public override void Write(byte[] buffer, int offset, int count)
            {
                _writeLock.Wait();
                try
                {
                    _innerStream.Write(buffer, offset, count);
                }
                finally
                {
                    _writeLock.Release();
                }
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await _writeLock.WaitAsync(cancellationToken);
                try
                {
                    await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
                }
                finally 
                { 
                    _writeLock.Release(); 
                }
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await _writeLock.WaitAsync(cancellationToken);
                try
                {
                    await _innerStream.WriteAsync(buffer, cancellationToken);
                }
                finally
                {
                    _writeLock.Release();
                }
            }

            public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _innerStream.ReadAsync(buffer, cancellationToken);

            public override long Length => _innerStream.Length;

            public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

            public override void Flush() => _innerStream.Flush();            
            
            public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
            
            public override void SetLength(long value) => _innerStream.SetLength(value);

            public override void Close() => _innerStream.Close();

            public override ValueTask DisposeAsync() => _innerStream.DisposeAsync();
        }
    }
}
