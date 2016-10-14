using Xunit;
using System.IO;

namespace Rnwood.SmtpServer.Tests
{
    
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