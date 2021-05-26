using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lindhart.Utility.IO.Streaming;
using NUnit.Framework;

namespace Streaming.Test
{
    public class DelegatingStreamTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [TestCase("")]
        [TestCase("Simple text")]
        [TestCase("More advanced text, זרו \r\n Test again")]
        public async Task Given_StreamWithData_When_WrappedInDelegatingStream_Then_BehavesAsNormalStream(string testInput)
        {
            // Setup
            byte[] bytes = Encoding.UTF8.GetBytes(testInput);
            MemoryStream normalStream = new MemoryStream(bytes);
            CustomStreamWrapper wrapper = new CustomStreamWrapper(normalStream);
            using var reader = new StreamReader(wrapper);
            
            // Act and assert
            Assert.That(await reader.ReadToEndAsync(), Is.EqualTo(testInput));
        }

        private class CustomStreamWrapper : DelegatingStream
        {
            public CustomStreamWrapper(Stream innerStream) : base(innerStream)
            {
            }
        }
    }
}