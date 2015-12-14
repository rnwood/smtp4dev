using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Rnwood.SmtpServer.Tests
{
    public abstract class AbstractSessionTests
    {
        protected abstract IEditableSession GetSession();

        [TestMethod]
        public void AppendToLog()
        {
            IEditableSession session = GetSession();
            session.AppendToLog("Blah1");
            session.AppendToLog("Blah2");

            CollectionAssert.AreEqual(new[] { "Blah1", "Blah2", "" },
                                    session.GetLog().ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.None));
        }

        [TestMethod]
        public void GetMessages_InitiallyEmpty()
        {
            IEditableSession session = GetSession();
            Assert.AreEqual(0, session.GetMessages().Length);
        }

        [TestMethod]
        public void AddMessage()
        {
            IEditableSession session = GetSession();
            Mock<IMessage> message = new Mock<IMessage>();

            session.AddMessage(message.Object);

            Assert.AreEqual(1, session.GetMessages().Length);
            Assert.AreSame(message.Object, session.GetMessages()[0]);
        }
    }
}