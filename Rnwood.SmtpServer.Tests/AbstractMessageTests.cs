using System;
using System.IO;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    public abstract class AbstractMessageTests
    {
        [Test]
        public void AddTo()
        {
            IEditableMessage message = GetMessage();

            message.AddTo("foo@bar.com");
            message.AddTo("bar@foo.com");

            Assert.AreEqual(2, message.To.Length);
            Assert.AreEqual(message.To[0], "foo@bar.com");
            Assert.AreEqual(message.To[1], "bar@foo.com");
        }

        protected abstract IEditableMessage GetMessage();

        [Test]
        public void GetData_ForWriting_Accepted()
        {
            IEditableMessage message = GetMessage();

            byte[] writtenBytes = new byte[64 * 1024];
            new Random().NextBytes(writtenBytes);

            using (Stream stream = message.GetData(true))
            {
                stream.Write(writtenBytes, 0, writtenBytes.Length);
            }


            byte[] readBytes;
            using (Stream stream = message.GetData())
            {
                readBytes = new byte[stream.Length];
                stream.Read(readBytes, 0, readBytes.Length);
            }

            Assert.AreElementsEqual(writtenBytes, readBytes);
        }
    }
}