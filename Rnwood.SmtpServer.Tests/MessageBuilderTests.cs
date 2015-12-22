using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Rnwood.SmtpServer.Tests
{
    public abstract class MessageBuilderTests
    {
        [TestMethod]
        public void AddTo()
        {
            IMessageBuilder builder = GetInstance();

            builder.To.Add("foo@bar.com");
            builder.To.Add("bar@foo.com");

            Assert.AreEqual(2, builder.To.Count);
            Assert.AreEqual(builder.To.ElementAt(0), "foo@bar.com");
            Assert.AreEqual(builder.To.ElementAt(1), "bar@foo.com");
        }

        protected abstract IMessageBuilder GetInstance();

        [TestMethod]
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

            CollectionAssert.AreEqual(writtenBytes, readBytes);
        }
    }
}