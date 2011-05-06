using System;

namespace Rnwood.SmtpServer
{
    public class RandomIntegerGenerator : IRandomIntegerGenerator
    {
        private static Random _random = new Random();

        public int GenerateRandomInteger(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }
    }
}