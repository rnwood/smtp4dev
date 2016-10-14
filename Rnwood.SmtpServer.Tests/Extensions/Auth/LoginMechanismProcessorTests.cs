using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    public class LoginMechanismProcessorTests : AuthMechanismTest
    {
        [Fact]
        public void ProcessRepsonse_NoUsername_GetUsernameChallenge()
        {
            Mocks mocks = new Mocks();

            LoginMechanismProcessor processor = Setup(mocks);
            AuthMechanismProcessorStatus result = processor.ProcessResponse(null);

            Assert.Equal(AuthMechanismProcessorStatus.Continue, result);
            mocks.Connection.Verify(c =>
                c.WriteResponse(
                        It.Is<SmtpResponse>(r =>
                           r.Code == (int)StandardSmtpResponseCode.AuthenticationContinue &&
                           VerifyBase64Response(r.Message, "Username:")
                        )
                )
            );
        }

        [Fact]
        public void ProcessRepsonse_Username_GetPasswordChallenge()
        {
            Mocks mocks = new Mocks();

            LoginMechanismProcessor processor = Setup(mocks);
            AuthMechanismProcessorStatus result = processor.ProcessResponse(EncodeBase64("rob"));

            Assert.Equal(AuthMechanismProcessorStatus.Continue, result);

            mocks.Connection.Verify(c =>
                c.WriteResponse(
                    It.Is<SmtpResponse>(r =>
                        VerifyBase64Response(r.Message, "Password:")
                        && r.Code == (int)StandardSmtpResponseCode.AuthenticationContinue
                    )
                )
            );
        }

        [Fact]
        public void ProcessResponse_Response_BadBase64()
        {
            Assert.Throws<BadBase64Exception>(() =>
            {
                Mocks mocks = new Mocks();

                LoginMechanismProcessor processor = Setup(mocks);
                processor.ProcessResponse(null);
                processor.ProcessResponse("rob blah");
            });
        }

        private LoginMechanismProcessor Setup(Mocks mocks)
        {
            return new LoginMechanismProcessor(mocks.Connection.Object);
        }
    }
}