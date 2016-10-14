using Xunit;
using Moq;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    
    public class MailFromVerbTests
    {
        [Fact]
        public void Process_AlreadyGivenFrom_ErrorResponse()
        {
            Mocks mocks = new Mocks();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(new Mock<IMessageBuilder>().Object);

            MailFromVerb mailFromVerb = new MailFromVerb();
            mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM <foo@bar.com>"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
        }

        [Fact]
        public void Process_MissingAddress_ErrorResponse()
        {
            Mocks mocks = new Mocks();

            MailFromVerb mailFromVerb = new MailFromVerb();
            mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
        }

        [Fact]
        public void Process_Address_Plain()
        {
            Process_Address("rob@rnwood.co.uk", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK);
        }

        [Fact]
        public void Process_Address_Bracketed()
        {
            Process_Address("<rob@rnwood.co.uk>", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK);
        }

        [Fact]
        public void Process_Address_BracketedWithName()
        {
            Process_Address("<Robert Wood <rob@rnwood.co.uk>>", "Robert Wood <rob@rnwood.co.uk>", StandardSmtpResponseCode.OK);
        }

        private void Process_Address(string address, string expectedParsedAddress, StandardSmtpResponseCode expectedResponse)
        {
            Mocks mocks = new Mocks();
            Mock<IMessageBuilder> message = new Mock<IMessageBuilder>();
            IMessageBuilder currentMessage = null;
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