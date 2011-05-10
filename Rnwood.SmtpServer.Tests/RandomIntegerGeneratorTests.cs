using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class RandomIntegerGeneratorTests
    {
        [Test]
        public void GenerateRandomInteger()
        {
            RandomIntegerGenerator randomNumberGenerator = new RandomIntegerGenerator();
            Assert.Between(randomNumberGenerator.GenerateRandomInteger(-100, 100), -100, 100);
        }
    }
}
