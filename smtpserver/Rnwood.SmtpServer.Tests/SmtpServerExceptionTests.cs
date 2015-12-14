using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class SmtpServerExceptionTests
    {
        [TestMethod]
        public void InnerException()
        {
            Exception innerException = new Exception();

            SmtpServerException e = new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Blah"), innerException);

            Assert.AreSame(innerException, e.InnerException);
        }

        [TestMethod]
        public void SmtpResponse()
        {
            SmtpResponse smtpResponse = new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Blah");
            SmtpServerException e = new SmtpServerException(smtpResponse);

            Assert.AreSame(smtpResponse, e.SmtpResponse);
        }
    }
}