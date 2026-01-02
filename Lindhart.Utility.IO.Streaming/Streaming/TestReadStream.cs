using System;
using System.IO;
using System.Text;

namespace Lindhart.Utility.IO.Streaming
{
    /// <summary>
    /// A usefull testing stream for reading. This will adhere to the minimum requirements of a readable <see cref="Stream"/>. This will behave as a read forward only stream, that is not always able to deliver all of the requested bytes. 
    /// </summary>
    public class TestReadStream : Stream
    {
        private const string NotSupported = "Not supported on this test stream, simulating real world streams";

        public TestReadStream(Stream stream)
        {
            if (stream?.CanRead != true)
                throw new ArgumentException("stream must be readable");

            _innerStream = stream;
        }

        public TestReadStream(byte[] bytes) : this(new MemoryStream(bytes))
        { }

        /// <summary>
        /// This will convert the string to UTF8 bytes
        /// </summary>
        /// <param name="input"></param>
        public TestReadStream(string input) : this(new MemoryStream(Encoding.UTF8.GetBytes(input)))
        { }

        private readonly Stream _innerStream;
        private readonly Random _random = new Random();

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException(NotSupported);

        public override long Position { get => throw new NotSupportedException(NotSupported); set => throw new NotSupportedException(NotSupported); }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length)
               throw new ArgumentOutOfRangeException(nameof(count), "The sum of offset and count is larger than the buffer length.");

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Must not be negative");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Must be higher than 0");

            int randomCount = count == 0 ? 0 : _random.Next(1, count);
            
            return _innerStream.Read(buffer, offset, randomCount);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(NotSupported);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("This is a read only stream");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("This is a read only stream");
        }
    }
}
