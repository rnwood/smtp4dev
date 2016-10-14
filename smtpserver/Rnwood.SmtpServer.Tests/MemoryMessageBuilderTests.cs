using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    
    public class MemoryMessageBuilderTests : MessageBuilderTests
    {
        protected override IMessageBuilder GetInstance()
        {
            Mocks mocks = new Mocks();
            return new MemoryMessage.Builder();
        }
    }
}