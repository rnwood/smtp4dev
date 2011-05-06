using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class MemorySessionTests : AbstractSessionTests
    {
        protected override IEditableSession GetSession()
        {
            return new MemorySession(IPAddress.Loopback, DateTime.Now);
        }
    }
}
