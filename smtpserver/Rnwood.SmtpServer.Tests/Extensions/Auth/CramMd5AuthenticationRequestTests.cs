using Xunit;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    
    public class CramMd5AuthenticationRequestTests
    {
        [Fact]
        public void ValidateResponse_Valid()
        {
            CramMd5AuthenticationCredentials authenticationCredentials = new CramMd5AuthenticationCredentials("username", "challenge", "b26eafe32c337296f7870c68edd5e8a5");
            Assert.True(authenticationCredentials.ValidateResponse("password"));
        }

        [Fact]
        public void ValidateResponse_Invalid()
        {
            CramMd5AuthenticationCredentials authenticationCredentials = new CramMd5AuthenticationCredentials("username", "challenge", "b26eafe32c337296f7870c68edd5e8a5");
            Assert.False(authenticationCredentials.ValidateResponse("password2"));
        }
    }
}