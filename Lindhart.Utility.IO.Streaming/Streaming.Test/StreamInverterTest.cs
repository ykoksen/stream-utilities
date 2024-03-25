using System.IO;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO.Compression;

namespace Lindhart.Utility.IO.Streaming
{

    [Parallelizable(ParallelScope.All)]
    public class StreamInverterTest
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(100)]
        [TestCase(20_000)]
        [TestCase(1_000_000)]
        public async Task Given_Stream_When_CompressedAndUncompressed_Then_ContentIsCorrect(long length)
        {
            // Setup
            // Large test stream, used just as generator. It's put into memory stream, so we can compare later
            await using LargeTestStream largeStream = new(length);
            await using MemoryStream testInput = new();
            await largeStream.CopyToAsync(testInput);
            testInput.Position = 0;
            // Wrap the test input in our TestReadStream that adhere to Stream interface, but is trying to tease us.
            await using var testReadStream = new TestReadStream(testInput);

            await using MemoryStream outputCompressedStream = new();

            await using var subject = new StreamInverter(testReadStream, s => new GZipStream(s, CompressionLevel.Optimal));

            // Act
            await subject.CopyToAsync(outputCompressedStream, 8000);
            outputCompressedStream.Position = 0;

            // Assert
            // We then revert the process in order to test it
            await using var outputTest = new MemoryStream();
            await using var uncompress = new GZipStream(outputCompressedStream, CompressionMode.Decompress);
            await uncompress.CopyToAsync(outputTest, 8000);

            Assert.That(outputTest.Length, Is.EqualTo(length));

            // If this is not the case, then this takes far too long
            if (length < 50_000)
                CollectionAssert.AreEquivalent(testInput.ToArray(), outputTest.ToArray());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(100)]
        [TestCase(20_000)]
        [TestCase(1_000_000)]
        public async Task Given_Stream_When_CompressedAndUncompressedUsingBrotliCompression_Then_ContentIsCorrect(long length)
        {
            // Setup
            // Large test stream, used just as generator. It's put into memory stream, so we can compare later
            await using LargeTestStream largeStream = new LargeTestStream(length);
            await using MemoryStream testInput = new MemoryStream();
            await largeStream.CopyToAsync(testInput);
            testInput.Position = 0;
            // Wrap the test input in our TestReadStream that adhere to Stream interface, but is trying to tease us.
            await using var testReadStream = new TestReadStream(testInput);

            await using MemoryStream outputCompressedStream = new();

            await using var subject = new StreamInverter(testReadStream, s => new BrotliStream(s, CompressionLevel.Optimal));

            // Act
            await subject.CopyToAsync(outputCompressedStream, 8000);
            outputCompressedStream.Position = 0;

            // Assert
            // We then revert the process in order to test it
            await using var outputTest = new MemoryStream();
            await using var uncompress = new BrotliStream(outputCompressedStream, CompressionMode.Decompress);
            await uncompress.CopyToAsync(outputTest, 8000);

            Assert.That(outputTest.Length, Is.EqualTo(length));

            // If this is not the case, then this takes far too long
            if (length < 50_000)
                CollectionAssert.AreEquivalent(testInput.ToArray(), outputTest.ToArray());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(100)]
        [TestCase(20_000)]
        [TestCase(1_000_000)]
        public void Given_Stream_When_CompressedAndUncompressedUsingBrotliCompressionSync_Then_ContentIsCorrect(long length)
        {
            // Setup
            // Large test stream, used just as generator. It's put into memory stream, so we can compare later
            using LargeTestStream largeStream = new LargeTestStream(length);
            using MemoryStream testInput = new MemoryStream();
            largeStream.CopyTo(testInput);
            testInput.Position = 0;
            // Wrap the test input in our TestReadStream that adhere to Stream interface, but is trying to tease us.
            using var testReadStream = new TestReadStream(testInput);

            using MemoryStream outputCompressedStream = new();

            using var subject = new StreamInverter(testReadStream, s => new BrotliStream(s, CompressionLevel.Optimal));

            // Act
            subject.CopyTo(outputCompressedStream, 8000);
            outputCompressedStream.Position = 0;

            // Assert
            // We then revert the process in order to test it
            using var outputTest = new MemoryStream();
            using var uncompress = new BrotliStream(outputCompressedStream, CompressionMode.Decompress);
            uncompress.CopyTo(outputTest, 8000);

            Assert.That(outputTest.Length, Is.EqualTo(length));

            // If this is not the case, then this takes far too long
            if (length < 50_000)
                CollectionAssert.AreEquivalent(testInput.ToArray(), outputTest.ToArray());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(100)]
        public async Task Given_Stream_When_TransferredUsingSmallOffByOneBuffers_Then_ContentIsCorrect(long length)
        {
            // Setup
            // Large test stream, used just as generator. It's put into memory stream, so we can compare later
            await using LargeTestStream largeStream = new LargeTestStream(length);
            await using MemoryStream testInput = new MemoryStream();
            await largeStream.CopyToAsync(testInput);
            testInput.Position = 0;
            await using MemoryStream outputStream = new();

            await using var subject = new StreamInverter(testInput, s => s, 33);

            // Act
            await subject.CopyToAsync(outputStream, 32);
            outputStream.Position = 0;

            // Assert
            Assert.That(outputStream.Length, Is.EqualTo(length));

            // If this is not the case, then this takes far too long
            if (length < 50_000)
                CollectionAssert.AreEquivalent(testInput.ToArray(), outputStream.ToArray());
        }
    }
}
