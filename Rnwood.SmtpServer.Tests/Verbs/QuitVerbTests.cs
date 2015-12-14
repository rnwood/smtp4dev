using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestClass]
    public class QuitVerbTests
    {
        [TestMethod]
        public void Quit_RespondsWithClosingChannel()
        {
            Mocks mocks = new Mocks();

            QuitVerb quitVerb = new QuitVerb();
            quitVerb.Process(mocks.Connection.Object, new SmtpCommand("QUIT"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.ClosingTransmissionChannel);
        }
    }
}