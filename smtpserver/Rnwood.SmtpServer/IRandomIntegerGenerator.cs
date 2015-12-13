using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public interface IRandomIntegerGenerator
    {
        int GenerateRandomInteger(int minValue, int maxValue);
    }
}
