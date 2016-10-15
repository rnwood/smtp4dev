using Moq;
using Rnwood.SmtpServer.Extensions.Auth;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Extensions.Auth
{
    public class CramMd5MechanismProcessorTests : AuthMechanismTest
    {
        [Fact]
        public async Task ProcessRepsonse_GetChallenge()
        {
            Mocks mocks = new Mocks();

            CramMd5MechanismProcessor cramMd5MechanismProcessor = Setup(mocks);
            AuthMechanismProcessorStatus result = await cramMd5MechanismProcessor.ProcessResponseAsync(null);

            string expectedResponse = string.Format("{0}.{1}@{2}", FAKERANDOM, FAKEDATETIME, FAKEDOMAIN);

            Assert.Equal(AuthMechanismProcessorStatus.Continue, result);
            mocks.Connection.Verify(
                    c => c.WriteResponseAsync(
                        It.Is<SmtpResponse>(r =>
                            r.Code == (int)StandardSmtpResponseCode.AuthenticationContinue &&
                            VerifyBase64Response(r.Message, expectedResponse)
                        )
                    )
                );
        }

        [Fact]
        public async Task ProcessRepsonse_ChallengeReponse_BadFormat()
        {
            await Assert.ThrowsAsync<SmtpServerException>(async () =>
            {
                Mocks mocks = new Mocks();

                string challenge = string.Format("{0}.{1}@{2}", FAKERANDOM, FAKEDATETIME, FAKEDOMAIN);

                CramMd5MechanismProcessor cramMd5MechanismProcessor = Setup(mocks, challenge);
                AuthMechanismProcessorStatus result = await cramMd5MechanismProcessor.ProcessResponseAsync("BLAH");
            });
        }

        [Fact]
        public async Task ProcessResponse_Response_BadBase64()
        {
            await Assert.ThrowsAsync<BadBase64Exception>(async () =>
            {
                Mocks mocks = new Mocks();

                CramMd5MechanismProcessor cramMd5MechanismProcessor = Setup(mocks);
                await cramMd5MechanismProcessor.ProcessResponseAsync(null);
                await cramMd5MechanismProcessor.ProcessResponseAsync("rob blah");
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