namespace Rnwood.SmtpServer
{
    public interface IRandomIntegerGenerator
    {
        int GenerateRandomInteger(int minValue, int maxValue);
    }
}