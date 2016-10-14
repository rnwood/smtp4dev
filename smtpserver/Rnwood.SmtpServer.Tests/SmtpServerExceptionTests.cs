using Xunit;
using System;

namespace Rnwood.SmtpServer.Tests
{
    
    public class SmtpServerExceptionTests
    {
        [Fact]
        public void InnerException()
        {
            Exception innerException = new Exception();

            SmtpServerException e = new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Blah"), innerException);

            Assert.Same(innerException, e.InnerException);
        }

        [Fact]
        public void SmtpResponse()
        {
            SmtpResponse smtpResponse = new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Blah");
            SmtpServerException e = new SmtpServerException(smtpResponse);

            Assert.Same(smtpResponse, e.SmtpResponse);
        }
    }
}