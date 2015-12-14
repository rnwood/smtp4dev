using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rnwood.SmtpServer.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    [TestClass]
    public class StartTlsVerbTests
    {
        [TestMethod]
        public void NoCertificateAvailable_ReturnsErrorResponse()
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.Setup(b => b.GetSSLCertificate(It.IsAny<IConnection>())).Returns<X509Certificate>(null);

            StartTlsVerb verb = new StartTlsVerb();
            verb.Process(mocks.Connection.Object, new SmtpCommand("STARTTLS"));

            mocks.VerifyWriteResponse(StandardSmtpResponseCode.CommandNotImplemented);
        }
    }
}