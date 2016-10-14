using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    
    public class QuitVerbTests
    {
        [Fact]
        public void Quit_RespondsWithClosingChannel()
        {
            Mocks mocks = new Mocks();

            QuitVerb quitVerb = new QuitVerb();
            quitVerb.Process(mocks.Connection.Object, new SmtpCommand("QUIT"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.ClosingTransmissionChannel);
        }
    }
}