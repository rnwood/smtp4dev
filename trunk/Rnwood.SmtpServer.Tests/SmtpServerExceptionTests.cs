using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
