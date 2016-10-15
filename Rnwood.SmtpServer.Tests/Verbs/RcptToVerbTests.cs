using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class RcptToVerbTests
    {
        [Fact]
        public async Task EmailAddressOnly()
        {
            await TestGoodAddressAsync("<rob@rnwood.co.uk>", "rob@rnwood.co.uk");
        }

        [Fact]
        public async Task EmailAddressWithDisplayName()
        {
            //Should this format be accepted????
            await TestGoodAddressAsync("<Robert Wood<rob@rnwood.co.uk>>", "Robert Wood<rob@rnwood.co.uk>");
        }

        private async Task TestGoodAddressAsync(string address, string expectedAddress)
        {
            Mocks mocks = new Mocks();
            MemoryMessage.Builder messageBuilder = new MemoryMessage.Builder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);

            RcptToVerb verb = new RcptToVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("TO " + address));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
            Assert.Equal(expectedAddress, messageBuilder.To.First());
        }

        [Fact]
        public async Task UnbraketedAddress_ReturnsError()
        {
            await TestBadAddressAsync("rob@rnwood.co.uk");
        }

        [Fact]
        public async Task MismatchedBraket_ReturnsError()
        {
            await TestBadAddressAsync("<rob@rnwood.co.uk");
            await TestBadAddressAsync("<Robert Wood<rob@rnwood.co.uk>");
        }

        [Fact]
        public async Task EmptyAddress_ReturnsError()
        {
            await TestBadAddressAsync("<>");
        }

        private async Task TestBadAddressAsync(string address)
        {
            Mocks mocks = new Mocks();
            MemoryMessage.Builder messageBuilder = new MemoryMessage.Builder();
            mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);

            RcptToVerb verb = new RcptToVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("TO " + address));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
            Assert.Equal(0, messageBuilder.To.Count);
        }
    }
}