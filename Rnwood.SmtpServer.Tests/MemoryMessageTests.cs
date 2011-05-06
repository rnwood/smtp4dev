using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class MemoryMessageTests : AbstractMessageTests
    {
        protected override IEditableMessage GetMessage()
        {
            Mocks mocks = new Mocks();
            return new MemoryMessage(mocks.Session.Object);
        }
    }
}
