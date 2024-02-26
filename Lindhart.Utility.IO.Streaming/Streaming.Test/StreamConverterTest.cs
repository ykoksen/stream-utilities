using System.IO;
using Lindhart.Utility.IO.Streaming;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO.Compression;

namespace Streaming.Test
{
    public class StreamConverterTest
    {
        [Test]
        public async Task TestIt()
        {
            long length = 20_000;
            await using LargeTestStream largeStream = new LargeTestStream(length);
            await using MemoryStream testStream = new MemoryStream();
            await using MemoryStream testCompressedStream = new MemoryStream();

            await largeStream.CopyToAsync(testStream);
            testStream.Position = 0;

            await using var testReadStream = new TestReadStream(testStream);
            await using var compressed = new StreamInverter(testReadStream, s => new GZipStream(s, CompressionLevel.Optimal, true));

            await compressed.CopyToAsync(testCompressedStream, 8000);

            testCompressedStream.Position = 0;

            await using var outputTest = new MemoryStream();

            await using var uncompress = new GZipStream(testCompressedStream, CompressionMode.Decompress);

            await uncompress.CopyToAsync(outputTest, 8000);

            Assert.That(outputTest.Length, Is.EqualTo(length));
            CollectionAssert.AreEquivalent(testStream.ToArray(), outputTest.ToArray());
        }
    }
}
