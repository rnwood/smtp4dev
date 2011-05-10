using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Moq;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestFixture]
    public class MailFromVerbTests
    {
        [Test]
        public void Process_AlreadyGivenFrom_ErrorResponse()
        {
            Mocks mocks = new Mocks();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(new Mock<IEditableMessage>().Object);

            MailFromVerb mailFromVerb = new MailFromVerb();
            mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM <foo@bar.com>"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
        }

        [Test]
        public void Process_MissingAddress_ErrorResponse()
        {
            Mocks mocks = new Mocks();

            MailFromVerb mailFromVerb = new MailFromVerb();
            mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
        }

        [Row("rob@rnwood.co.uk", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK)]
        [Row("<rob@rnwood.co.uk>", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK)]
        [Row("<Robert Wood <rob@rnwood.co.uk>>", "Robert Wood <rob@rnwood.co.uk>", StandardSmtpResponseCode.OK)]
        [Test]
        public void Process_Address(string address, string expectedParsedAddress, StandardSmtpResponseCode expectedResponse)
        {
            Mocks mocks = new Mocks();
            Mock<IEditableMessage> message = new Mock<IEditableMessage>();
            IEditableMessage currentMessage = null;
            mocks.Connection.Setup(c => c.NewMessage()).Returns(() =>
                                                                                                        {
                                                                                                            currentMessage
                                                                                                                =
                                                                                                                message.
                                                                                                                    Object;
                                                                                                            return
                                                                                                                currentMessage;
                                                                                                        }
        );
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(() => currentMessage);

            MailFromVerb mailFromVerb = new MailFromVerb();
            mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM " + address));

            mocks.VerifyWriteResponse(expectedResponse);
            message.VerifySet(m => m.From = expectedParsedAddress);
        }
    }
}
