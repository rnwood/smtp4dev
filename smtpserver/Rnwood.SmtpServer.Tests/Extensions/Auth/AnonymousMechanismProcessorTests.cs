using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    public class AnomymousMechanismProcessorTests
    {
        [Fact]
        public async Task ProcessResponse_Success()
        {
            await ProcessResponseAsync(AuthenticationResult.Success, AuthMechanismProcessorStatus.Success);
        }

        [Fact]
        public async Task ProcessResponse_Failure()
        {
            await ProcessResponseAsync(AuthenticationResult.Failure, AuthMechanismProcessorStatus.Failed);
        }

        [Fact]
        public async Task ProcessResponse_TemporarilyFailure()
        {
            await ProcessResponseAsync(AuthenticationResult.TemporaryFailure, AuthMechanismProcessorStatus.Failed);
        }

        private async Task ProcessResponseAsync(AuthenticationResult authenticationResult, AuthMechanismProcessorStatus authMechanismProcessorStatus)
        {
            Mocks mocks = new Mocks();
            mocks.ServerBehaviour.Setup(
                b =>
                b.ValidateAuthenticationCredentialsAsync(mocks.Connection.Object, It.IsAny<AnonymousAuthenticationCredentials>()))
                .ReturnsAsync(authenticationResult);

            AnonymousMechanismProcessor anonymousMechanismProcessor = new AnonymousMechanismProcessor(mocks.Connection.Object);
            AuthMechanismProcessorStatus result = await anonymousMechanismProcessor.ProcessResponseAsync(null);

            Assert.Equal(authMechanismProcessorStatus, result);

            if (authenticationResult == AuthenticationResult.Success)
            {
                Assert.IsType<AnonymousAuthenticationCredentials>(anonymousMechanismProcessor.Credentials);
            }
        }
    }
}