using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    [TestFixture]
    public class CramMd5AuthenticationRequestTests
    {
        [Test]
        public void ValidateResponse_Valid()
        {
            CramMd5AuthenticationRequest authenticationRequest = new CramMd5AuthenticationRequest("username", "challenge", "b26eafe32c337296f7870c68edd5e8a5");
            Assert.IsTrue(authenticationRequest.ValidateResponse("password"));
        }

        [Test]
        public void ValidateResponse_Invalid()
        {
            CramMd5AuthenticationRequest authenticationRequest = new CramMd5AuthenticationRequest("username", "challenge", "b26eafe32c337296f7870c68edd5e8a5");
            Assert.IsFalse(authenticationRequest.ValidateResponse("password2"));
        }
    }
}
