using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Lindhart.Utility.IO.Streaming
{
    public class TestEndlessStreamTest
    {
        [Test]
        public void Given_Stream_When_Read_Then_ReturnsRequestedCountAndAdvancesPosition()
        {
            using var stream = new TestEndlessStream();
            var buffer = new byte[32];

            var r1 = stream.Read(buffer, 0, buffer.Length);
            Assert.That(r1, Is.EqualTo(32));
            Assert.That(stream.Position, Is.EqualTo(32));

            var r2 = stream.Read(buffer, 0, 10);
            Assert.That(r2, Is.EqualTo(10));
            Assert.That(stream.Position, Is.EqualTo(42));
        }

        [Test]
        public void Given_Stream_When_InvalidReadArgs_Then_Throws()
        {
            using var stream = new TestEndlessStream();
            var buffer = new byte[8];

            Assert.Throws<ArgumentNullException>(() => stream.Read(null!, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, 0, -1));
            Assert.Throws<ArgumentException>(() => stream.Read(buffer, 6, 3)); // 6+3 > 8
        }

        [Test]
        public void Given_Stream_When_CapabilitiesQueried_Then_CorrectFlags()
        {
            using var stream = new TestEndlessStream();
            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.CanWrite, Is.False);
            Assert.That(stream.CanSeek, Is.False);
        }

        [Test]
        public void Given_Stream_When_LengthOrSeekOrSetPosition_Then_NotSupported()
        {
            using var stream = new TestEndlessStream();
            Assert.Throws<NotSupportedException>(() =>
            {
                var _ = stream.Length;
            });
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.Position = 5);
        }

        [Test]
        public void Given_Stream_When_WriteOrSetLength_Then_NotSupported()
        {
            using var stream = new TestEndlessStream();
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(10));
        }

        [Test]
        public void Given_SimulateEnd_When_Read_Then_ReturnsZeroAndPositionUnchanged()
        {
            using var stream = new TestEndlessStream();
            var buffer = new byte[16];

            var r1 = stream.Read(buffer, 0, 8);
            Assert.That(r1, Is.EqualTo(8));
            Assert.That(stream.Position, Is.EqualTo(8));

            stream.SimulateEndOfStream();

            var r2 = stream.Read(buffer, 0, 8);
            Assert.That(r2, Is.EqualTo(0));
            Assert.That(stream.Position, Is.EqualTo(8));
        }

        [Test]
        public void Given_Read_When_BytesAreRandom_Then_TwoReadsProduceDifferentDataMostLikely()
        {
            using var stream = new TestEndlessStream();
            var b1 = new byte[32];
            var b2 = new byte[32];

            stream.Read(b1, 0, b1.Length);
            stream.Read(b2, 0, b2.Length);

            // Not a strict guarantee but highly likely for Random.NextBytes
            Assert.That(b1, Is.Not.EqualTo(b2));
        }

        [Test]
        public async Task Given_ReadingEndlessStream_When_SimulatingEnd_Then_ReadingStops()
        {
            await using var subject = new TestEndlessStream();
            await using var counter = new StreamCounter(Stream.Null);
            var task = subject.CopyToAsync(counter);

            await Task.Delay(TimeSpan.FromMilliseconds(500));
            Assert.That(task.IsCompleted, Is.False);

            subject.SimulateEndOfStream();

            await task;

            Assert.That(counter.BytesWritten, Is.GreaterThan(0));
        }

        [Test]
        public async Task Given_ReadingEndlessStream_When_ReadByMemoryStream_Then_OutOfMemoryException()
        {
            await using var subject = new TestEndlessStream();
            await using var memoryStream = new MemoryStream();

            // MemoryStream has a fixed limit of 2GB, where it throws the below exception
            Assert.ThrowsAsync<IOException>(() => subject.CopyToAsync(memoryStream));
        }

        [Test]
        public async Task Given_IncorrectlyUsedHttpClient_When_ReturningEndlessStream_Then_ExceptionDueToBufferingAll()
        {
            await using var subject = new TestEndlessStream();
            using var client = new HttpClient(new MockedHttpHandler(subject));

            Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("http://example.com"));
        }

        private class MockedHttpHandler : HttpMessageHandler
        {
            private readonly Stream _stream;

            public MockedHttpHandler(Stream stream)
            {
                _stream = stream;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                System.Threading.CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StreamContent(_stream)
                };

                return Task.FromResult(response);
            }
        }
    }
}
