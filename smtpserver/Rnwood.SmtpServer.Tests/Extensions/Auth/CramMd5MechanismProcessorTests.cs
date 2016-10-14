using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using System;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    public class CramMd5MechanismProcessorTests : AuthMechanismTest
    {
        [Fact]
        public void ProcessRepsonse_GetChallenge()
        {
            Mocks mocks = new Mocks();

            CramMd5MechanismProcessor cramMd5MechanismProcessor = Setup(mocks);
            AuthMechanismProcessorStatus result = cramMd5MechanismProcessor.ProcessResponse(null);

            string expectedResponse = string.Format("{0}.{1}@{2}", FAKERANDOM, FAKEDATETIME, FAKEDOMAIN);

            Assert.Equal(AuthMechanismProcessorStatus.Continue, result);
            mocks.Connection.Verify(
                    c => c.WriteResponse(
                        It.Is<SmtpResponse>(r =>
                            r.Code == (int)StandardSmtpResponseCode.AuthenticationContinue &&
                            VerifyBase64Response(r.Message, expectedResponse)
                        )
                    )
                );
        }

        [Fact]
        public void ProcessRepsonse_ChallengeReponse_BadFormat()
        {
            Assert.Throws<SmtpServerException>(() =>
            {
                Mocks mocks = new Mocks();

                string challenge = string.Format("{0}.{1}@{2}", FAKERANDOM, FAKEDATETIME, FAKEDOMAIN);

                CramMd5MechanismProcessor cramMd5MechanismProcessor = Setup(mocks, challenge);
                AuthMechanismProcessorStatus result = cramMd5MechanismProcessor.ProcessResponse("BLAH");
            });
        }

        [Fact]
        public void ProcessResponse_Response_BadBase64()
        {
            Assert.Throws<BadBase64Exception>(() =>
            {
                Mocks mocks = new Mocks();

                CramMd5MechanismProcessor cramMd5MechanismProcessor = Setup(mocks);
                cramMd5MechanismProcessor.ProcessResponse(null);
                cramMd5MechanismProcessor.ProcessResponse("rob blah");
            });
        }

        private const int FAKEDATETIME = 10000;
        private const int FAKERANDOM = 1234;
        private const string FAKEDOMAIN = "mockdomain";

        private CramMd5MechanismProcessor Setup(Mocks mocks, string challenge = null)
        {
            Mock<IRandomIntegerGenerator> randomMock = new Mock<IRandomIntegerGenerator>();
            randomMock.Setup(r => r.GenerateRandomInteger(It.IsAny<int>(), It.IsAny<int>())).Returns(FAKERANDOM);

            Mock<ICurrentDateTimeProvider> dateMock = new Mock<ICurrentDateTimeProvider>();
            dateMock.Setup(d => d.GetCurrentDateTime()).Returns(new DateTime(FAKEDATETIME));

            mocks.ServerBehaviour.SetupGet(b => b.DomainName).Returns(FAKEDOMAIN);

            return new CramMd5MechanismProcessor(mocks.Connection.Object, randomMock.Object, dateMock.Object, challenge);
        }
    }
}