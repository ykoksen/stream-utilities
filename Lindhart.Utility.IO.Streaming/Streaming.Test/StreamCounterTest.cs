using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lindhart.Utility.IO.Streaming
{
    public class StreamCounterTest
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1_000_000_000)]
        [TestCase(241_943_135)]
        public async Task Given_StreamWithGivenLength_When_Read_Then_CorrectNumberOfBytesRead(long length)
        {
            // Setup
            using TestLargeInputStream testStream = new(length);
            using var subject = new StreamCounter(new StupidStream(testStream));

            // Act
            await subject.CopyToAsync(Stream.Null);

            // Assert
            Assert.That(subject.BytesRead, Is.EqualTo(length));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1_000_000_000)]
        [TestCase(241_943_135)]
        public async Task Given_StreamWithGivenLength_When_Write_Then_CorrectNumberOfBytesRead(long length)
        {
            // Setup
            using TestLargeInputStream testStream = new(length);
            using var subject = new StreamCounter(Stream.Null);

            // Act
            await testStream.CopyToAsync(subject);

            // Assert
            Assert.That(subject.BytesWritten, Is.EqualTo(length));
        }

        [Test]
        public async Task Given_HugeStream_When_ReadingBytesWithDifferentMethods_Then_CorrectNumberOfBytesCounted()
        {
            // Setup
            using TestLargeInputStream testStream = new(2_000_000_000);
            using var subject = new StreamCounter(testStream);
            var readCounter = 0;

            // Act + Assertions

            // Read byte
            subject.ReadByte();
            Assert.That(subject.BytesRead, Is.EqualTo(++readCounter));

            // Read array
            subject.Read(new byte[50000], 35, 42);
            Assert.That(subject.BytesRead, Is.EqualTo(readCounter += 42));

            // Read array async
            await subject.ReadAsync(new byte[50000], 35, 42);
            Assert.That(subject.BytesRead, Is.EqualTo(readCounter += 42));

            // Read memory
            subject.Read(new Span<byte>(new byte[55]));
            Assert.That(subject.BytesRead, Is.EqualTo(readCounter += 55));

            // Read memory async
            await subject.ReadAsync(new Memory<byte>(new byte[55]));
            Assert.That(subject.BytesRead, Is.EqualTo(readCounter += 55));

            // Read using the old async methods
            TaskCompletionSource<IAsyncResult> source = new();
            subject.BeginRead(new byte[50000], 234, 9384, r => source.SetResult(r), this);
            subject.EndRead(await source.Task);
            Assert.That(subject.BytesRead, Is.EqualTo(readCounter += 9384));
        }

        [Test]
        public async Task Given_NullStream_When_WritingBytesWithDifferentMethods_Then_CorrectNumberOfBytesCounted()
        {
            // Setup
            using var subject = new StreamCounter(Stream.Null);
            var writeCounter = 0;

            // Act + Assertions

            // Write byte
            subject.WriteByte(12);
            Assert.That(subject.BytesWritten, Is.EqualTo(++writeCounter));

            // Write array
            subject.Write(new byte[50000], 35, 42);
            Assert.That(subject.BytesWritten, Is.EqualTo(writeCounter += 42));

            // Write array async
            await subject.WriteAsync(new byte[50000], 35, 42);
            Assert.That(subject.BytesWritten, Is.EqualTo(writeCounter += 42));

            // Write memory
            subject.Write(new Span<byte>(new byte[55]));
            Assert.That(subject.BytesWritten, Is.EqualTo(writeCounter += 55));

            // Write memory async
            await subject.WriteAsync(new Memory<byte>(new byte[55]));
            Assert.That(subject.BytesWritten, Is.EqualTo(writeCounter += 55));

            // Write using the old async methods
            TaskCompletionSource<IAsyncResult> source = new();
            subject.BeginWrite(new byte[50000], 234, 9384, r => source.SetResult(r), this);
            subject.EndWrite(await source.Task);
            Assert.That(subject.BytesWritten, Is.EqualTo(writeCounter += 9384));
        }

        private sealed class TestReadScope : IDisposable
        {
            public StreamCounter Subject { get; }
            public TestLargeInputStream TestStream { get; }

            public TestReadScope(long length)
            {
                TestStream = new TestLargeInputStream(length);
                Subject = new StreamCounter(TestStream);
            }

            public void Dispose()
            {
                Subject.Dispose();
                TestStream.Dispose();

                GC.SuppressFinalize(this);
            }
        }
    }
}
