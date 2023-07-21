﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Test
{
    public class LargeTestStream : Stream
    {
        private Random _random;

        private long _bytesLeft;

        public LargeTestStream(long NumberOfBytes)
        {
            TotalBytes = _bytesLeft = NumberOfBytes;
            _random = new Random();
        }

        public long TotalBytes { get; }

        public long BytesRead => TotalBytes - _bytesLeft;
                
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var back = (int)Math.Min(count, _bytesLeft);

            if (back == buffer.Length && offset == 0)
            {
                _random.NextBytes(buffer);
            }
            else
            {
                for (int i = 0; i < back; i++)
                {
                    buffer[offset + i] = (byte)_random.Next(0, 255);
                }
            }

            _bytesLeft -= back;

            return back;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var back = Read(buffer, offset, count);
            return Task.FromResult(back);
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
    }
}
