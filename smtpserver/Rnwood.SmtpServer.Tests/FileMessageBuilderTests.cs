using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class FileMessageBuilderTests : MessageBuilderTests
    {
        protected override IMessageBuilder GetInstance()
        {
            FileInfo tempFile = new FileInfo(Path.GetTempFileName());

            Mocks mocks = new Mocks();
            return new FileMessage.Builder(tempFile, false);
        }
    }
}