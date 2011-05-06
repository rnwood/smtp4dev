using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class SmtpServerExceptionTests
    {
        [Test]
        public void InnerException()
        {
            Exception innerException = new Exception();

            SmtpServerException e = new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Blah"), innerException);

            Assert.AreSame(innerException, e.InnerException);
        }

        [Test]
        public void SmtpResponse()
        {
            SmtpResponse smtpResponse = new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Blah");
            SmtpServerException e = new SmtpServerException(smtpResponse);

            Assert.AreSame(smtpResponse, e.SmtpResponse);
        }
    }
}
