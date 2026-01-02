using System;
using System.IO;

namespace Lindhart.Utility.IO.Streaming
{
    /// <summary>
    /// A useful testing stream for testing large input scenarios.
    /// This stream does not actually hold any data, but can be used to simulate reading from a large stream.
    /// </summary>
    public class TestEndlessStream : Stream
    {
        private readonly Random _random = new Random();
        private long _position;
        private volatile bool simulateEnd;

        public override void Flush()
        {
            // No-op: read-only, non-buffered random stream
        }

        /// <summary>
        /// This will simulate the end of the stream on all subsequent reads.
        /// </summary>
        public void SimulateEndOfStream()
        {
            simulateEnd = true;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (simulateEnd)
            {
                return 0;
            }

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException(offset < 0 ? nameof(offset) : nameof(count), "Must be non-negative.");
            if (offset + count > buffer.Length)
                throw new ArgumentException("The sum of offset and count is larger than the buffer length.");

            _random.NextBytes(buffer.AsSpan(offset, count));

            _position += count;

            return count;
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
            throw new NotSupportedException("This stream does not support writing");
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }
    }
}
