using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MbUnit.Framework;
using Moq;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestFixture]
    public class EhloVerbTests
    {
        [Test]
        public void Process_RespondsWith250()
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
            ehloVerb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);

        }

        [Test]
        public void Process_NoArguments_Accepted()
        {
            Mocks mocks = new Mocks();
            EhloVerb ehloVerb = new EhloVerb();
            ehloVerb.Process(mocks.Connection.Object, new SmtpCommand("EHLO"));
            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);

            mocks.Session.VerifySet(s => s.ClientName = "");
        }


        [Test]
        public void Process_RecordsClientName()
        {
            Mocks mocks = new Mocks();
            EhloVerb ehloVerb = new EhloVerb();
            ehloVerb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

            mocks.Session.VerifySet(s => s.ClientName = "foobar");
        }

        [Test]
        public void Process_RespondsWithExtensionKeywords()
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
            ehloVerb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

            mocks.Connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r =>

                r.Message.Contains("EXTN1") &&
                r.Message.Contains("EXTN2A") &&
                    r.Message.Contains("EXTN2B")
                )));

        }

        [Test]
        public void Process_SaidHeloAlready_Allowed()
        {
            Mocks mocks = new Mocks();

            EhloVerb verb = new EhloVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foo.blah"));
            verb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foo.blah"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        }
    }
}
