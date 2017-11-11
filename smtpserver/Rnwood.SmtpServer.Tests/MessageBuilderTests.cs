using Xunit;
using System;
using System.IO;
using System.Linq;

namespace Rnwood.SmtpServer.Tests
{
    public abstract class MessageBuilderTests
    {
        [Fact]
        public void AddTo()
        {
            IMessageBuilder builder = GetInstance();

            builder.To.Add("foo@bar.com");
            builder.To.Add("bar@foo.com");

            Assert.Equal(2, builder.To.Count);
            Assert.Equal("foo@bar.com", builder.To.ElementAt(0));
            Assert.Equal("bar@foo.com", builder.To.ElementAt(1));
        }

        protected abstract IMessageBuilder GetInstance();

        [Fact]
        public void WriteData_Accepted()
        {
            IMessageBuilder builder = GetInstance();

            byte[] writtenBytes = new byte[64 * 1024];
            new Random().NextBytes(writtenBytes);

            using (Stream stream = builder.WriteData())
            {
                stream.Write(writtenBytes, 0, writtenBytes.Length);
            }

            byte[] readBytes;
            using (Stream stream = builder.GetData())
            {
                readBytes = new byte[stream.Length];
                stream.Read(readBytes, 0, readBytes.Length);
            }

            Assert.Equal(writtenBytes, readBytes);
        }
    }
}