using FluentAssertions;
using Rnwood.Smtp4dev.Tests.Resources;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Server.CertificateHelper
{
    public class CertificateHelperTests
    {
        [Fact]
        public void CanLoadCertificateAndKey()
        {
            var certificatePath = ResourceHelper.GetResourcePath("smtp4dev.crt");
            var certificateKeyPath = ResourceHelper.GetResourcePath("smtp4dev.key");

            var cert = Rnwood.Smtp4dev.Server.CertificateHelper.LoadCertificateWithKey(certificatePath, certificateKeyPath, "");

            cert.Should().NotBeNull();
            cert.HasPrivateKey.Should().BeTrue();
        }
    }
}