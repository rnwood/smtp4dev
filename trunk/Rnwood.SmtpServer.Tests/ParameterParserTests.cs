using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class ParameterParserTests
    {
        [TestMethod]
        public void NoParameters()
        {
            ParameterParser parameterParser = new ParameterParser("");

            Assert.AreEqual(0, parameterParser.Parameters.Length);
        }

        [TestMethod]
        public void SingleParameter()
        {
            ParameterParser parameterParser = new ParameterParser("KEYA=VALUEA");

            Assert.AreEqual(1, parameterParser.Parameters.Length);
            Assert.AreEqual(new Parameter("KEYA", "VALUEA"), parameterParser.Parameters[0]);
        }

        [TestMethod]
        public void MultipleParameters()
        {
            ParameterParser parameterParser = new ParameterParser("KEYA=VALUEA KEYB=VALUEB");

            Assert.AreEqual(2, parameterParser.Parameters.Length);
            Assert.AreEqual(new Parameter("KEYA", "VALUEA"), parameterParser.Parameters[0]);
            Assert.AreEqual(new Parameter("KEYB", "VALUEB"), parameterParser.Parameters[1]);
        }
    }
}
