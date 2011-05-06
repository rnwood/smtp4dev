using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using Moq;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    [TestFixture]
    public class AnomymousMechanismProcessorTests
    {
        [Test]
        public void ProcessResponse_Success()
        {
            ProcessResponse(AuthenticationResult.Success, AuthMechanismProcessorStatus.Success);
        }

        [Test]
        public void ProcessResponse_Failure()
        {
            ProcessResponse(AuthenticationResult.Failure, AuthMechanismProcessorStatus.Failed);
        }

        [Test]
        public void ProcessResponse_TemporarilyFailure()
        {
            ProcessResponse(AuthenticationResult.TemporaryFailure, AuthMechanismProcessorStatus.Failed);
        }

        private void ProcessResponse(AuthenticationResult authenticationResult, AuthMechanismProcessorStatus authMechanismProcessorStatus)
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.Setup(
                b =>
                b.ValidateAuthenticationCredentials(mocks.Connection.Object, It.IsAny<AnonymousAuthenticationRequest>()))
                .Returns(authenticationResult);

            AnonymousMechanismProcessor anonymousMechanismProcessor = new AnonymousMechanismProcessor(mocks.Connection.Object);
            AuthMechanismProcessorStatus result = anonymousMechanismProcessor.ProcessResponse("");

            Assert.AreEqual(authMechanismProcessorStatus, result);

            if (authenticationResult == AuthenticationResult.Success)
            {
                Assert.IsInstanceOfType(typeof(AnonymousAuthenticationRequest), anonymousMechanismProcessor.Credentials);
            }
        }
    }
}
