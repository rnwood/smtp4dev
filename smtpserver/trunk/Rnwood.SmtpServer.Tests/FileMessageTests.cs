using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
