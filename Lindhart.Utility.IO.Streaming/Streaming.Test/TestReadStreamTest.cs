using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lindhart.Utility.IO.Streaming;
using NUnit.Framework;

namespace Streaming.Test
{
    public class TestReadStreamTest
    {
        [Test]
        public void Given_SimpleTextIntoTestStream_When_ReadingAllStream_Then_ContentSame()
        {
            // Setup
            string testText = "Simple small text for testing the content of the stream is absolutely correct, without any problem whatsoever. This text should not be too small so therefore it is written to be somewhat long, tada taha, whatever. I think I'll end it now though.";

            var subject = new TestReadStream(testText);

            // Act
            var reader = new StreamReader(subject, Encoding.UTF8, false, 10);
            var output = reader.ReadToEnd();

            // Assert
            Assert.That(output, Is.EqualTo(testText));
        }

        [Test]
        public async Task Given_SimpleTextIntoTestStream_When_ReadingStream_Then_NotGettingAllRequestedBytes()
        {
            // Run the code 10 times, because we could be unlucky that it randomly select to acutally read all 10 bytes - but if we try 10 times then it is very unlikely
            List<int> numbersOfReadBytes = new();
            for (int i = 0; i < 10; i++)
            {
                numbersOfReadBytes.Add(await ReadFirstChunk());
            }

            // Assert (we take the lowest value that should be enough)
            Assert.That(numbersOfReadBytes.Aggregate(Math.Min), Is.Not.EqualTo(10));
        }

        [Test]
        public void Given_SimpleTextIntoTestStream_When_SettingPosition_Then_GetNotSupportedException()
        {
            // Setup
            string testText = "Simple small text for testing the content of the stream is absolutely correct, without any problem whatsoever. This text should not be too small so therefore it is written to be somewhat long, tada taha, whatever. I think I'll end it now though.";

            var subject = new TestReadStream(testText);

            Assert.Throws<NotSupportedException>(() => subject.Position = 0);
        }

        private static async Task<int> ReadFirstChunk()
        {
            // Setup
            string testText = "Simple small text for testing the content of the stream is absolutely correct, without any problem whatsoever. This text should not be too small so therefore it is written to be somewhat long, tada taha, whatever. I think I'll end it now though.";

            var subject = new TestReadStream(testText);
            Memory<byte> buffer = new byte[10];

            // Act
            var numberOfBytesRead = await subject.ReadAsync(buffer);
            return numberOfBytesRead;
        }
    }
}
