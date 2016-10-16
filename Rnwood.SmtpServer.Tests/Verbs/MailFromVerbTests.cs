using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class MailFromVerbTests
    {
        [Fact]
        public async Task Process_AlreadyGivenFrom_ErrorResponse()
        {
            Mocks mocks = new Mocks();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(new Mock<IMessageBuilder>().Object);

            MailFromVerb mailFromVerb = new MailFromVerb();
            await mailFromVerb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("FROM <foo@bar.com>"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.BadSequenceOfCommands);
        }

        [Fact]
        public async Task Process_MissingAddress_ErrorResponse()
        {
            Mocks mocks = new Mocks();

            MailFromVerb mailFromVerb = new MailFromVerb();
            await mailFromVerb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("FROM"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
        }

        [Fact]
        public async Task Process_Address_Plain()
        {
            await Process_AddressAsync("rob@rnwood.co.uk", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK);
        }

        [Fact]
        public async Task Process_Address_Bracketed()
        {
            await Process_AddressAsync("<rob@rnwood.co.uk>", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK);
        }

        [Fact]
        public async Task Process_Address_BracketedWithName()
        {
            await Process_AddressAsync("<Robert Wood <rob@rnwood.co.uk>>", "Robert Wood <rob@rnwood.co.uk>", StandardSmtpResponseCode.OK);
        }

        private async Task Process_AddressAsync(string address, string expectedParsedAddress, StandardSmtpResponseCode expectedResponse)
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
            });
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(() => currentMessage);

            MailFromVerb mailFromVerb = new MailFromVerb();
            await mailFromVerb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("FROM " + address));

            mocks.VerifyWriteResponseAsync(expectedResponse);
            message.VerifySet(m => m.From = expectedParsedAddress);
        }
    }
}