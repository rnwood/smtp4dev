using Rnwood.SmtpServer.Verbs;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class RsetVerbTests
    {
        [Fact]
        public async Task ProcessAsync()
        {
            Mocks mocks = new Mocks();

            RsetVerb verb = new RsetVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("RSET"));

            await mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
            mocks.Connection.Verify(c => c.AbortMessage());
        }
    }
}