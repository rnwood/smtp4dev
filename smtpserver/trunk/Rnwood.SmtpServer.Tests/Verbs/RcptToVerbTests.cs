using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Moq;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestFixture]
    public class RcptToVerbTests
    {
        [Test]
        public void EmailAddressOnly()
        {
            TestGoodAddress("<rob@rnwood.co.uk>", "rob@rnwood.co.uk");
        }

        [Test]
        public void EmailAddressWithDisplayName()
        {
            //Should this format be accepted????
            TestGoodAddress("<Robert Wood<rob@rnwood.co.uk>>", "Robert Wood<rob@rnwood.co.uk>");
        }

        private void TestGoodAddress(string address, string expectedAddress)
        {
            Mocks mocks = new Mocks();
            Message message = new Message(mocks.Session.Object);
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(message);

            RcptToVerb verb = new RcptToVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("TO " + address));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
            Assert.AreEqual(expectedAddress, message.To[0]);
        }

        [Test]
        public void UnbraketedAddress_ReturnsError()
        {
            TestBadAddress("rob@rnwood.co.uk");
        }

        [Test]
        public void MismatchedBraket_ReturnsError()
        {
            TestBadAddress("<rob@rnwood.co.uk");
            TestBadAddress("<Robert Wood<rob@rnwood.co.uk>");
        }

        [Test]
        public void EmptyAddress_ReturnsError()
        {
            TestBadAddress("<>");
        }

        private void TestBadAddress(string address)
        {
            Mocks mocks = new Mocks();
            Message message = new Message(mocks.Session.Object);
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(message);

            RcptToVerb verb = new RcptToVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("TO " + address));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
            Assert.AreEqual(0, message.To.Length);
        }
    }
}
