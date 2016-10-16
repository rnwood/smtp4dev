using Moq;
using Rnwood.SmtpServer.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs
{
    public class StartTlsVerbTests
    {
        [Fact]
        public async Task NoCertificateAvailable_ReturnsErrorResponse()
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.Setup(b => b.GetSSLCertificate(It.IsAny<IConnection>())).Returns<X509Certificate>(null);

            StartTlsVerb verb = new StartTlsVerb();
            await verb.ProcessAsync(mocks.Connection.Object, new SmtpCommand("STARTTLS"));

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.CommandNotImplemented);
        }
    }
}