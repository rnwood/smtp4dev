using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestClass]
    public class NoopVerbTests
    {
        [TestMethod]
        public void Noop()
        {
            Mocks mocks = new Mocks();

            NoopVerb verb = new NoopVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("NOOP"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }
    }
}