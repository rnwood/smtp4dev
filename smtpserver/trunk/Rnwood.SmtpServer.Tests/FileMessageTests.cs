using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
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
