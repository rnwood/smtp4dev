using Moq;
using Rnwood.SmtpServer.Extensions;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class EhloVerbTests
    {
        [Fact]
        public async Task Process_RespondsWith250()
        {
            Mocks mocks = new Mocks();
            Mock<IExtensionProcessor> mockExtensionProcessor1 = new Mock<IExtensionProcessor>();
            mockExtensionProcessor1.SetupGet(ep => ep.EHLOKeywords).Returns(new[] { "EXTN1" });
            Mock<IExtensionProcessor> mockExtensionProcessor2 = new Mock<IExtensionProcessor>();
            mockExtensionProcessor2.SetupGet(ep => ep.EHLOKeywords).Returns(new[] { "EXTN2A", "EXTN2B" });

            mocks.Connection.SetupGet(c => c.ExtensionProcessors).Returns(new[]
                                                                              {
                                                                                  mockExtensionProcessor1.Object,
                                                                                  mockExtensionProcessor2.Object
                                                                              });

            EhloVerb ehloVerb = new EhloVerb();
            await ehloVerb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
        }

        [Fact]
        public async Task Process_NoArguments_Accepted()
        {
            Mocks mocks = new Mocks();
            EhloVerb ehloVerb = new EhloVerb();
            await ehloVerb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("EHLO"));
            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);

            mocks.Session.VerifySet(s => s.ClientName = "");
        }

        [Fact]
        public async Task Process_RecordsClientName()
        {
            Mocks mocks = new Mocks();
            EhloVerb ehloVerb = new EhloVerb();
            await ehloVerb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

            mocks.Session.VerifySet(s => s.ClientName = "foobar");
        }

        [Fact]
        public async Task Process_RespondsWithExtensionKeywords()
        {
            Mocks mocks = new Mocks();
            Mock<IExtensionProcessor> mockExtensionProcessor1 = new Mock<IExtensionProcessor>();
            mockExtensionProcessor1.SetupGet(ep => ep.EHLOKeywords).Returns(new[] { "EXTN1" });
            Mock<IExtensionProcessor> mockExtensionProcessor2 = new Mock<IExtensionProcessor>();
            mockExtensionProcessor2.SetupGet(ep => ep.EHLOKeywords).Returns(new[] { "EXTN2A", "EXTN2B" });

            mocks.Connection.SetupGet(c => c.ExtensionProcessors).Returns(new[]
                                                                              {
                                                                                  mockExtensionProcessor1.Object,
                                                                                  mockExtensionProcessor2.Object
                                                                              });

            EhloVerb ehloVerb = new EhloVerb();
            await ehloVerb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

            mocks.Connection.Verify(c => c.WriteResponseAsync(It.Is<SmtpResponse>(r =>

                r.Message.Contains("EXTN1") &&
                r.Message.Contains("EXTN2A") &&
                    r.Message.Contains("EXTN2B")
                )));
        }

        [Fact]
        public async Task Process_SaidHeloAlready_Allowed()
        {
            Mocks mocks = new Mocks();

            EhloVerb verb = new EhloVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("EHLO foo.blah"));
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("EHLO foo.blah"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
        }
    }
}