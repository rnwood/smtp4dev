using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    
    public class RandomIntegerGeneratorTests
    {
        [Fact]
        public void GenerateRandomInteger()
        {
            RandomIntegerGenerator randomNumberGenerator = new RandomIntegerGenerator();
            int randomNumber = randomNumberGenerator.GenerateRandomInteger(-100, 100);
            Assert.True(randomNumber >= -100);
            Assert.True(randomNumber <= 100);
        }
    }
}