using System;
using System.IO;

namespace Lindhart.Utility.IO.Streaming
{
    /// <summary>
    /// A useful testing stream for testing large input scenarios.
    /// This stream does not actually hold any data, but can be used to simulate reading from a large stream.
    /// </summary>
    public class TestLargeInputStream : Stream
    {
        private readonly long _length;
        private readonly Random _random;
        private long _position;

        /// <summary>
        /// Initializes a new instance of the TestLargeInputStream class with the specified stream length.
        /// </summary>
        /// <param name="length">The total length, in bytes, of the stream to simulate. Must be non-negative.</param>
        /// <param name="seed">The seed for the random bytes used in simulation. Using the same seed twice generates the same bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if length is less than zero.</exception>
        public TestLargeInputStream(long length, int? seed = null)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");

            _length = length;
            _random = seed != null ? new Random(seed.Value) : new Random();
            _position = 0;
        }

        public override void Flush()
        {
            // No-op: read-only, non-buffered random stream
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException(offset < 0 ? nameof(offset) : nameof(count), "Must be non-negative.");
            if (offset + count > buffer.Length)
                throw new ArgumentException("The sum of offset and count is larger than the buffer length.");

            if (_position >= _length || count == 0)
                return 0;

            var remaining = _length - _position;
            var toRead = (int)Math.Min(count, remaining);

            _random.NextBytes(buffer.AsSpan(offset, toRead));

            _position += toRead;
            return toRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("This stream does not support seeking");
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

        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException("This stream does not support setting the position");
        }
    }
}
