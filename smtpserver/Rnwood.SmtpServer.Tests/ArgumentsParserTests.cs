using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class ArgumentsParserTests
    {
        [TestMethod]
        public void Parsing_FirstArgumentAferVerbWithColon_Split()
        {
            ArgumentsParser args = new ArgumentsParser("ARG1=VALUE:BLAH");
            Assert.AreEqual(1, args.Arguments.Length);
            Assert.AreEqual("ARG1=VALUE:BLAH", args.Arguments[0]);
        }

        [TestMethod]
        public void Parsing_MailFrom_WithDisplayName()
        {
            ArgumentsParser args = new ArgumentsParser("<Robert Wood<rob@rnwood.co.uk>> ARG1 ARG2");
            Assert.AreEqual("<Robert Wood<rob@rnwood.co.uk>>", args.Arguments[0]);
            Assert.AreEqual("ARG1", args.Arguments[1]);
            Assert.AreEqual("ARG2", args.Arguments[2]);
        }

        [TestMethod]
        public void Parsing_MailFrom_EmailOnly()
        {
            ArgumentsParser args = new ArgumentsParser("<rob@rnwood.co.uk> ARG1 ARG2");
            Assert.AreEqual("<rob@rnwood.co.uk>", args.Arguments[0]);
            Assert.AreEqual("ARG1", args.Arguments[1]);
            Assert.AreEqual("ARG2", args.Arguments[2]);
        }
    }
}