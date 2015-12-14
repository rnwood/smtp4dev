using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class FileMessageTests : AbstractMessageTests
    {
        protected override IEditableMessage GetMessage()
        {
            FileInfo tempFile = new FileInfo(Path.GetTempFileName());

            Mocks mocks = new Mocks();
            return new FileMessage(mocks.Session.Object, tempFile, false);
        }
    }
}