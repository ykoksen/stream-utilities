using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lindhart.Utility.IO.Streaming
{
    /// <summary>
    /// A class to wrap around an existing <see cref="Stream"/>. This will use a thread to enable processing content from the buffer, while reading new bytes from the underlying <see cref="Stream"/>. 
    /// This is especially usefull when reading from a network (hence <see cref="System.Net.Sockets.NetworkStream"/>) or reading from a <see cref="Stream"/> that does another job, example compressing or calculating hash.
    /// </summary>
    public class BufferBackgroundStream3 : Stream
    {
        private const int DefaultBufferSize = 100_000;

        /// <summary>
        /// The stream that is wrapped
        /// </summary>
        private readonly Stream _innerStream;

        private readonly Buffer _buffer1;
        private readonly Buffer _buffer2;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly Task _backGroundWorker;

        /// <summary>
        /// True if we should read from buffer 1, false if we should read from buffer 2
        /// </summary>
        private bool _buffer1ReadActive;
                
        private long _position;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerStream">The <see cref="Stream"/> to wrap and read from.</param>
        /// <param name="bufferSize">The size of the buffer. Note that double this size will be used since there will be a buffer to read from, while another buffer can be used in the background to fetch data in parallel. 
        /// The default buffer size is 100 KB (thereby using 200 KB memory)</param>
        public BufferBackgroundStream3(Stream innerStream, int bufferSize = DefaultBufferSize)
        {
            _buffer1ReadActive = true;
            _innerStream = innerStream;

            _buffer1 = new Buffer(bufferSize);
            _buffer2 = new Buffer(bufferSize);
            _cancellationTokenSource = new CancellationTokenSource();

            _backGroundWorker = BackgroundFillBuffersTask(_cancellationTokenSource.Token);
        }

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
            var activeBuffer = _buffer1ReadActive ? _buffer1 : _buffer2;

            int readCount = await activeBuffer.ReadAsync(buffer, cancellationToken);
            _position += readCount;

            if (readCount != 0)
            {
                return readCount;
            }
            else
            {
                if (activeBuffer.EndOfStream)
                {
                    return 0;
                }

                // Reached the end of current buffer - switching to the other one
                _buffer1ReadActive = !_buffer1ReadActive;

                // We've reached the end of one of the buffers and just call the method recursively hitting the other buffer
                return await ReadAsync(buffer, cancellationToken);
            }            
        }

        private async Task BackgroundFillBuffersTask(CancellationToken token)
        {
            while (!_buffer1.EndOfStream && !_buffer2.EndOfStream && !token.IsCancellationRequested)
            {
                await _buffer1.WriteBuffer(_innerStream, token);

                await _buffer2.WriteBuffer(_innerStream, token);
            }
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
