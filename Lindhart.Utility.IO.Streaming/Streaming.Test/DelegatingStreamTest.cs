using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lindhart.Utility.IO.Streaming;
using NSubstitute;
using NUnit.Framework;

namespace Streaming.Test
{
    public class DelegatingStreamTest
    {
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

        [Test]
        public void When_MethodCalled_Then_UnderlyingStreamMethodCalled()
        {
            var t = new TestScope();
            var byteSpan = new Span<byte>(new byte[10]);
            t.Subject.Read(new byte[10], 15, 32);

            t.Stream.Received(1).Read(Arg.Any<byte[]>(), 15, 32);
        }

        private class TestScope
        {
            public CustomStreamWrapper Subject { get; }
            public Stream Stream { get; }

            public TestScope()
            {
                Stream = Substitute.For<Stream>();
                Subject = new CustomStreamWrapper(Stream);
            }
        }



        private class CustomStreamWrapper : DelegatingStream
        {
            public CustomStreamWrapper(Stream innerStream) : base(innerStream)
            {
            }
        }
    }
}