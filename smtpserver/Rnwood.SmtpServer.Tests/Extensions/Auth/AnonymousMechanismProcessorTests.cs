using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    public class AnomymousMechanismProcessorTests
    {
        [Fact]
        public void ProcessResponse_Success()
        {
            ProcessResponse(AuthenticationResult.Success, AuthMechanismProcessorStatus.Success);
        }

        [Fact]
        public void ProcessResponse_Failure()
        {
            ProcessResponse(AuthenticationResult.Failure, AuthMechanismProcessorStatus.Failed);
        }

        [Fact]
        public void ProcessResponse_TemporarilyFailure()
        {
            ProcessResponse(AuthenticationResult.TemporaryFailure, AuthMechanismProcessorStatus.Failed);
        }

        private async Task ProcessResponse(AuthenticationResult authenticationResult, AuthMechanismProcessorStatus authMechanismProcessorStatus)
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.Setup(
                b =>
                b.ValidateAuthenticationCredentials(mocks.Connection.Object, It.IsAny<AnonymousAuthenticationCredentials>()))
                .Returns(authenticationResult);

            AnonymousMechanismProcessor anonymousMechanismProcessor = new AnonymousMechanismProcessor(mocks.Connection.Object);
            AuthMechanismProcessorStatus result = await anonymousMechanismProcessor.ProcessResponseAsync(null);

            Assert.Equal(authMechanismProcessorStatus, result);

            if (authenticationResult == AuthenticationResult.Success)
            {
                Assert.IsType(typeof(AnonymousAuthenticationCredentials), anonymousMechanismProcessor.Credentials);
            }
        }
    }
}