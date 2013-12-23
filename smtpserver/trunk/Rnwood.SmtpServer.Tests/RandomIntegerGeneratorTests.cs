using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class RandomIntegerGeneratorTests
    {
        [TestMethod]
        public void GenerateRandomInteger()
        {
            RandomIntegerGenerator randomNumberGenerator = new RandomIntegerGenerator();
            int randomNumber = randomNumberGenerator.GenerateRandomInteger(-100, 100);
            Assert.IsTrue(randomNumber >= -100);
            Assert.IsTrue(randomNumber <= 100);
        }
    }
}
