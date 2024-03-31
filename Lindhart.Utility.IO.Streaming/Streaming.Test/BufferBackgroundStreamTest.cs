using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
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
            var stopReadingFromInnerStreamFlag = new TaskCompletionSource();
            var hasReadFourTimesFlag = new TaskCompletionSource();
            var testInnerStream = new DelegateInAsyncReadStream(new LargeTestStream(100), async () =>
            {
                if (++numberOfCalls == 2) 
                { 
                    await stopReadingFromInnerStreamFlag.Task;
                }

                if (numberOfCalls == 4)
                {
                    hasReadFourTimesFlag.TrySetResult();
                }
            });

            var subject = new BufferBackgroundStream(testInnerStream, 50);

            // Act
            // Should be able to do this twice, since we're reading from the buffer (this uses the first buffer)
            await subject.ReadAsync(readBuffer);
            await subject.ReadAsync(readBuffer);

            // We now try to read from the second buffer, but writing to it is not yet complete as we've set a blocking task above
            var task = subject.ReadAsync(readBuffer);

            await Task.Delay(100);

            // Assert
            Assert.That(task.IsCompleted, Is.False);

            // We allow the second buffer to complete writing
            stopReadingFromInnerStreamFlag.TrySetResult();

            // We should now be able to read the second buffer as writing to it is complete
            await task;

            await Task.Delay(100);

            // Since buffer 2 and 3 are full we should not be reading into buffer 4
            Assert.That(hasReadFourTimesFlag.Task.IsCompleted, Is.False);
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

        /// <summary>
        /// This test enforces the buffer to sometimes wait for reading to finish before it can write, and to sometimes wait for writing to finish before it can read.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task Given_UnderlyingStreamThatReadsWithRandomDelay_When_Reading_Then_StreamContentIsCorrect()
        {
            // Setup
            var bytes = new byte[100_000];
            Random.Shared.NextBytes(bytes);

            var underlyingStream = new DelegateInAsyncReadStream(new TestReadStream(new MemoryStream(bytes)), async () => await Task.Delay(Random.Shared.Next(80)));

            var subject = new BufferBackgroundStream(underlyingStream, 9000);

            var outputStream = new MemoryStream();

            // Act
            var readBuffer = new Memory<byte>(new byte[8999]);
            var count = -1;
            while (count != 0)
            {
                await Task.Delay(Random.Shared.Next(80));
                count = await subject.ReadAsync(readBuffer);

                if (count != 0)
                    await outputStream.WriteAsync(readBuffer.Slice(0, count));
            }

            // Assert
            CollectionAssert.AreEqual(bytes, outputStream.ToArray());
        }

        private class DelegateInAsyncReadStream : DelegatingStream
        {
            private readonly Func<Task> _asyncDelegate;

            public DelegateInAsyncReadStream(Stream innerStream, Func<Task> asyncDelegate) : base(innerStream)
            {
                this._asyncDelegate = asyncDelegate;
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await _asyncDelegate();
                return await base.ReadAsync(buffer, offset, count, cancellationToken);
            }
        }
    }
}
