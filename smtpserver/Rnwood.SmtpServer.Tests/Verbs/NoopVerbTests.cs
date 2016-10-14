using Xunit;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    
    public class NoopVerbTests
    {
        [Fact]
        public void Noop()
        {
            Mocks mocks = new Mocks();

            NoopVerb verb = new NoopVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("NOOP"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }
    }
}