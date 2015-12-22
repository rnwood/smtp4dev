using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class MemoryMessageBuilderTests : MessageBuilderTests
    {
        protected override IMessageBuilder GetInstance()
        {
            Mocks mocks = new Mocks();
            return new MemoryMessage.Builder();
        }
    }
}