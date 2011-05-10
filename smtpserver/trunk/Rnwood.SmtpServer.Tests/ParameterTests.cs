using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class ParameterTests
    {
        [Test]
        public void Name()
        {
            Parameter p = new Parameter("name", "value");

            Assert.AreEqual("name", p.Name);
        }

        [Test]
        public void Value()
        {
            Parameter p = new Parameter("name", "value");

            Assert.AreEqual("value", p.Value);
        }

        [VerifyContract]
        public readonly EqualityContract<Parameter> Equality = new EqualityContract<Parameter>()
        {
            ImplementsOperatorOverloads =
                false,
            EquivalenceClasses =
                                                                                               {
                                                                                                   {
                                                                                                       new Parameter("KEYA", "VALUEA"),
                                                                                                       new Parameter("KEYa", "VALUEA")
                                                                                                    }, 
                                                                                                    {
                                                                                                       new Parameter("KEYB", "VALUEb"),
                                                                                                       new Parameter("KEYB", "VALUEb")
                                                                                                    }
                                                                                               }
        };
    }
}
