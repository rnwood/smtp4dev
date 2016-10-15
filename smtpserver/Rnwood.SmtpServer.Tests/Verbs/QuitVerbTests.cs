using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class QuitVerbTests
    {
        [Fact]
        public async Task Quit_RespondsWithClosingChannel()
        {
            Mocks mocks = new Mocks();

            QuitVerb quitVerb = new QuitVerb();
            await quitVerb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("QUIT"));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.ClosingTransmissionChannel);
        }
    }
}