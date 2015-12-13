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
    public class CramMd5MechanismProcessorTests
    {
        [TestMethod]
        public void ProcessRepsonse_GetChallenge()
        {
            Mocks mocks = new Mocks();

            CramMd5MechanismProcessor cramMd5MechanismProcessor = Setup(mocks);
            AuthMechanismProcessorStatus result = cramMd5MechanismProcessor.ProcessResponse(null);

            Assert.AreEqual(AuthMechanismProcessorStatus.Continue, result);
            mocks.Connection.Verify(c => c.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationContinue, "MTIzNC4xMDAwMEBtb2NrZG9tYWlu")));
        }

        [TestMethod]
        [ExpectedException(typeof(SmtpServerException))]
        public void ProcessResponse_Response_BadBase64()
        {
            Mocks mocks = new Mocks();

            CramMd5MechanismProcessor cramMd5MechanismProcessor = Setup(mocks);
            cramMd5MechanismProcessor.ProcessResponse(null);
            cramMd5MechanismProcessor.ProcessResponse("rob blah");
        }

        private CramMd5MechanismProcessor Setup(Mocks mocks)
        {
            Mock<IRandomIntegerGenerator> randomMock = new Mock<IRandomIntegerGenerator>();
            randomMock.Setup(r => r.GenerateRandomInteger(It.IsAny<int>(), It.IsAny<int>())).Returns(1234);

            Mock<ICurrentDateTimeProvider> dateMock = new Mock<ICurrentDateTimeProvider>();
            dateMock.Setup(d => d.GetCurrentDateTime()).Returns(new DateTime(10000));

            mocks.ServerBehaviour.SetupGet(b => b.DomainName).Returns("mockdomain");

            string challenge = "1234.10000@mockdomain";
            string challengeB64 = "MTIzNC4xMDAwMEBtb2NrZG9tYWlu";

            return new CramMd5MechanismProcessor(mocks.Connection.Object, randomMock.Object, dateMock.Object);
        }
    }
}
