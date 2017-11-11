using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    
    public class ParameterParserTests
    {
        [Fact]
        public void NoParameters()
        {
            ParameterParser parameterParser = new ParameterParser(new string[0]);

            Assert.Empty(parameterParser.Parameters);
        }

        [Fact]
        public void SingleParameter()
        {
            ParameterParser parameterParser = new ParameterParser("KEYA=VALUEA");

            Assert.Single(parameterParser.Parameters);
            Assert.Equal(new Parameter("KEYA", "VALUEA"), parameterParser.Parameters[0]);
        }

        [Fact]
        public void MultipleParameters()
        {
            ParameterParser parameterParser = new ParameterParser("KEYA=VALUEA", "KEYB=VALUEB");

            Assert.Equal(2, parameterParser.Parameters.Length);
            Assert.Equal(new Parameter("KEYA", "VALUEA"), parameterParser.Parameters[0]);
            Assert.Equal(new Parameter("KEYB", "VALUEB"), parameterParser.Parameters[1]);
        }
    }
}