using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestFixture]
    public class QuitVerbTests
    {
        [Test]
        public void Quit_RespondsWithClosingChannel()
        {
            Mocks mocks = new Mocks();

            QuitVerb quitVerb = new QuitVerb();
            quitVerb.Process(mocks.Connection.Object, new SmtpCommand("QUIT"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.ClosingTransmissionChannel);
        }
    }
}
