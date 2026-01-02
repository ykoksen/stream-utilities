using System;
using System.IO;
using NUnit.Framework;

namespace Lindhart.Utility.IO.Streaming
{
    public class TestLargeInputStreamTest
    {
        [Test]
        public void Given_NegativeLength_When_InitializingStream_Then_ThrowsArgumentOutOfRangeException()
        {
            // Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new TestLargeInputStream(-1));
        }

        [Test]
        public void Given_Length100_When_LengthPropertyAccessed_Then_Returns100()
        {
            using var stream = new TestLargeInputStream(100);
            Assert.That(stream.Length, Is.EqualTo(100));
        }

        [Test]
        public void Given_SeededStreams_When_ReadSameRanges_Then_ProduceSameBytes()
        {
            using var s1 = new TestLargeInputStream(64, seed: 42);
            using var s2 = new TestLargeInputStream(64, seed: 42);

            var b1 = new byte[32];
            var b2 = new byte[32];

            var r1 = s1.Read(b1, 0, b1.Length);
            var r2 = s2.Read(b2, 0, b2.Length);

            Assert.That(r1, Is.EqualTo(32));
            Assert.That(r2, Is.EqualTo(32));
            Assert.That(b1, Is.EqualTo(b2));

            // Read next range and compare again
            r1 = s1.Read(b1, 0, b1.Length);
            r2 = s2.Read(b2, 0, b2.Length);

            Assert.That(r1, Is.EqualTo(32));
            Assert.That(r2, Is.EqualTo(32));
            Assert.That(b1, Is.EqualTo(b2));
        }

        [Test]
        public void Given_Stream_When_Read_Then_PositionAdvancesAndStopsAtEnd()
        {
            using var stream = new TestLargeInputStream(10);
            var buffer = new byte[8];

            var r1 = stream.Read(buffer, 0, buffer.Length);
            Assert.That(r1, Is.EqualTo(8));
            Assert.That(stream.Position, Is.EqualTo(8));

            var r2 = stream.Read(buffer, 0, buffer.Length);
            Assert.That(r2, Is.EqualTo(2));
            Assert.That(stream.Position, Is.EqualTo(10));

            var r3 = stream.Read(buffer, 0, buffer.Length);
            Assert.That(r3, Is.EqualTo(0));
            Assert.That(stream.Position, Is.EqualTo(10));
        }

        [Test]
        public void Given_InvalidReadArgs_When_Read_Then_Throws()
        {
            using var stream = new TestLargeInputStream(10);
            var buffer = new byte[4];

            Assert.Throws<ArgumentNullException>(() => stream.Read(null!, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, 0, -1));
            Assert.Throws<ArgumentException>(() => stream.Read(buffer, 3, 2)); // 3+2 > 4
        }

        [Test]
        public void Given_Stream_When_CapabilitiesQueried_Then_CorrectFlags()
        {
            using var stream = new TestLargeInputStream(1);
            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.CanWrite, Is.False);
            Assert.That(stream.CanSeek, Is.False);
        }

        [Test]
        public void Given_Stream_When_WriteOrSetLength_Then_NotSupported()
        {
            using var stream = new TestLargeInputStream(1);
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(10));
        }
        
        [Test]
        public void Given_InputStream_When_Seek_Then_NotSupportedException()
        {
            using var stream = new TestLargeInputStream(50);
            Assert.Throws<NotSupportedException>(() => stream.Seek(12, SeekOrigin.Begin));
        }
    }
}
