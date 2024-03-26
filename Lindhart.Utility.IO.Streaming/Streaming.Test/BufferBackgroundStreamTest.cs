using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lindhart.Utility.IO.Streaming
{
    public class BufferBackgroundStreamTest
    {
        [Test]
        [Timeout(10000)]
        public async Task Given_ReadingFromUnderlyingStreamIsBlocked_When_Reading_Then_MustWaitForUnderlyingStreamToBeUnblocked()
        {
            // Setup
            var readBuffer = new Memory<byte>(new byte[25]);

            var numberOfCalls = 0;
            var flag = new TaskCompletionSource();
            var testInnerStream = new LargeTestStream(100, async () =>
            {
                if (++numberOfCalls == 2) 
                { 
                    await flag.Task;
                }
            });

            var subject = new BufferBackgroundStream(testInnerStream, 50);

            // Act
            // Should be able to do this twice, since we're reading from the buffer
            await subject.ReadAsync(readBuffer);
            await subject.ReadAsync(readBuffer);

            var task = subject.ReadAsync(readBuffer);

            await Task.Delay(100);

            // Assert
            Assert.That(task.IsCompleted, Is.False);

            flag.TrySetResult();

            await task;
        }

        [Test]
        public async Task Given_UnderlyingStreamThatReadsRandomNumberOfBytes_When_Reading_Then_StreamContentIsCorrect()
        {
            // Setup
            var bytes = new byte[100_000];
            Random.Shared.NextBytes(bytes);

            var underlyingStream = new TestReadStream(new MemoryStream(bytes));

            var subject = new BufferBackgroundStream(underlyingStream, 197);

            var outputStream = new MemoryStream();

            // Act
            await subject.CopyToAsync(outputStream);

            // Assert
            CollectionAssert.AreEqual(bytes, outputStream.ToArray());
        }

    }
}
