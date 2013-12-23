using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class ParameterTests
    {
        [TestMethod]
        public void Name()
        {
            Parameter p = new Parameter("name", "value");

            Assert.AreEqual("name", p.Name);
        }

        [TestMethod]
        public void Value()
        {
            Parameter p = new Parameter("name", "value");

            Assert.AreEqual("value", p.Value);
        }

        [TestMethod]
        public void Equality_Equal()
        {
            Assert.IsTrue(new Parameter("KEYA", "VALUEA").Equals( new Parameter("KEYa", "VALUEA")));
        }

        [TestMethod]
        public void Equality_NotEqual()
        {
            Assert.IsFalse(new Parameter("KEYb", "VALUEb").Equals(new Parameter("KEYa", "VALUEA")));
        }
    }
}
