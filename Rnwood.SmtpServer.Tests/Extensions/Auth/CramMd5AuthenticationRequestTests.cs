using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    [TestClass]
    public class CramMd5AuthenticationRequestTests
    {
        [TestMethod]
        public void ValidateResponse_Valid()
        {
            CramMd5AuthenticationCredentials authenticationCredentials = new CramMd5AuthenticationCredentials("username", "challenge", "b26eafe32c337296f7870c68edd5e8a5");
            Assert.IsTrue(authenticationCredentials.ValidateResponse("password"));
        }

        [TestMethod]
        public void ValidateResponse_Invalid()
        {
            CramMd5AuthenticationCredentials authenticationCredentials = new CramMd5AuthenticationCredentials("username", "challenge", "b26eafe32c337296f7870c68edd5e8a5");
            Assert.IsFalse(authenticationCredentials.ValidateResponse("password2"));
        }
    }
}
