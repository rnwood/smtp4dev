using FluentAssertions;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Server.SSCertGenerator
{
    public class CertificateTests
    {
        [Fact]
        public void CanGenerateSelfSignedCertificate()
        {
            var cert = Smtp4dev.Server.SSCertGenerator.CreateSelfSignedCertificate("localhost");
            cert.Should().NotBeNull();
        }
    }
}