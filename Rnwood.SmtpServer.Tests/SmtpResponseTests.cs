using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class SmtpResponseTests
    {
        [TestMethod]
        public void IsError_Error()
        {
            SmtpResponse r = new SmtpResponse(500, "An error happened");
            Assert.IsTrue(r.IsError);
        }

        [TestMethod]
        public void IsError_NotError()
        {
            SmtpResponse r = new SmtpResponse(200, "No error happened");
            Assert.IsFalse(r.IsError);
        }

        [TestMethod]
        public void IsSuccess_Error()
        {
            SmtpResponse r = new SmtpResponse(500, "An error happened");
            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void IsSuccess_NotError()
        {
            SmtpResponse r = new SmtpResponse(200, "No error happened");
            Assert.IsTrue(r.IsSuccess);
        }

        [TestMethod]
        public void Message()
        {
            SmtpResponse r = new SmtpResponse(1, "Blah");
            Assert.AreEqual("Blah", r.Message);
        }

        [TestMethod]
        public void Code()
        {
            SmtpResponse r = new SmtpResponse(1, "Blah");
            Assert.AreEqual(1, r.Code);
        }

        [TestMethod]
        public void ToString_SingleLineMessage()
        {
            SmtpResponse r = new SmtpResponse(200, "Single line message");
            Assert.AreEqual("200 Single line message\r\n", r.ToString());
        }

        [TestMethod]
        public void ToString_MultiLineMessage()
        {
            SmtpResponse r = new SmtpResponse(200, "Multi line message line 1\r\n" +
            "Multi line message line 2");
            Assert.AreEqual("200-Multi line message line 1\r\n" +
            "200 Multi line message line 2\r\n", r.ToString());
        }

        [TestMethod]
        public void Equality_Equal()
        {
            Assert.IsTrue(new SmtpResponse(StandardSmtpResponseCode.OK, "OK").Equals(new SmtpResponse(StandardSmtpResponseCode.OK, "OK")));
        }

        [TestMethod]
        public void Equality_NotEqual()
        {
            Assert.IsFalse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised, "Eror").Equals(new SmtpResponse(StandardSmtpResponseCode.OK, "OK")));
        }
    }
}