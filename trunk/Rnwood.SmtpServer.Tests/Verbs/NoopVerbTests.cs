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
    public class NoopVerbTests
    {
        [Test]
        public void Noop()
        {
            Mocks mocks = new Mocks();

            NoopVerb verb = new NoopVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("NOOP"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }
    }
}
