using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class SmtpResponseTests
    {
        [Test]
        public void IsError_Error()
        {
            SmtpResponse r = new SmtpResponse(500, "An error happened");
            Assert.IsTrue(r.IsError);
        }

        [Test]
        public void IsError_NotError()
        {
            SmtpResponse r = new SmtpResponse(200, "No error happened");
            Assert.IsFalse(r.IsError);
        }

        [Test]
        public void IsSuccess_Error()
        {
            SmtpResponse r = new SmtpResponse(500, "An error happened");
            Assert.IsFalse(r.IsSuccess);
        }

        [Test]
        public void IsSuccess_NotError()
        {
            SmtpResponse r = new SmtpResponse(200, "No error happened");
            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public void Message()
        {
            SmtpResponse r = new SmtpResponse(1, "Blah");
            Assert.AreEqual("Blah", r.Message);
        }


        [Test]
        public void Code()
        {
            SmtpResponse r = new SmtpResponse(1, "Blah");
            Assert.AreEqual(1, r.Code);
        }

        [Test]
        public void ToString_SingleLineMessage()
        {
            SmtpResponse r = new SmtpResponse(200, "Single line message");
            Assert.AreEqual("200 Single line message\r\n", r.ToString());
        }

        [Test]
        public void ToString_MultiLineMessage()
        {
            SmtpResponse r = new SmtpResponse(200, "Multi line message line 1\r\n" +
            "Multi line message line 2");
            Assert.AreEqual("200-Multi line message line 1\r\n" +
            "200 Multi line message line 2\r\n", r.ToString());
        }

        [VerifyContract]
        public readonly EqualityContract<SmtpResponse> Equality = new EqualityContract<SmtpResponse>()
                                                                      {
                                                                          ImplementsOperatorOverloads =
                                                                              false,
                                                                          EquivalenceClasses =
                                                                                               {
                                                                                                   {
                                                                                                       new SmtpResponse(StandardSmtpResponseCode.OK, "OK"),
                                                                                                       new SmtpResponse(StandardSmtpResponseCode.OK, "OK")
                                                                                                    }, 
                                                                                                    {
                                                                                                       new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Error"),
                                                                                                       new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Error")
                                                                                                    }
                                                                                               }
                                                                      };
    }
}
