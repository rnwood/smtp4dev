using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    [TestClass]
    public class AnomymousMechanismProcessorTests
    {
        [TestMethod]
        public void ProcessResponse_Success()
        {
            ProcessResponse(AuthenticationResult.Success, AuthMechanismProcessorStatus.Success);
        }

        [TestMethod]
        public void ProcessResponse_Failure()
        {
            ProcessResponse(AuthenticationResult.Failure, AuthMechanismProcessorStatus.Failed);
        }

        [TestMethod]
        public void ProcessResponse_TemporarilyFailure()
        {
            ProcessResponse(AuthenticationResult.TemporaryFailure, AuthMechanismProcessorStatus.Failed);
        }

        private void ProcessResponse(AuthenticationResult authenticationResult, AuthMechanismProcessorStatus authMechanismProcessorStatus)
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.Setup(
                b =>
                b.ValidateAuthenticationCredentials(mocks.Connection.Object, It.IsAny<AnonymousAuthenticationCredentials>()))
                .Returns(authenticationResult);

            AnonymousMechanismProcessor anonymousMechanismProcessor = new AnonymousMechanismProcessor(mocks.Connection.Object);
            AuthMechanismProcessorStatus result = anonymousMechanismProcessor.ProcessResponse(null);

            Assert.AreEqual(authMechanismProcessorStatus, result);

            if (authenticationResult == AuthenticationResult.Success)
            {
                Assert.IsInstanceOfType(anonymousMechanismProcessor.Credentials, typeof(AnonymousAuthenticationCredentials));
            }
        }
    }
}
