using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class HeloVerbTests
    {
        [Fact]
        public async Task SayHelo()
        {
            Mocks mocks = new Mocks();

            HeloVerb verb = new HeloVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
            mocks.Session.VerifySet(s => s.ClientName = "foo.blah");
        }

        [Fact]
        public async Task SayHeloTwice_ReturnsError()
        {
            Mocks mocks = new Mocks();
            mocks.Session.SetupGet(s => s.ClientName).Returns("already.said.helo");

            HeloVerb verb = new HeloVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.BadSequenceOfCommands);
        }

        [Fact]
        public async Task SayHelo_NoName()
        {
            Mocks mocks = new Mocks();

            HeloVerb verb = new HeloVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("HELO"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
            mocks.Session.VerifySet(s => s.ClientName = "");
        }
    }
}