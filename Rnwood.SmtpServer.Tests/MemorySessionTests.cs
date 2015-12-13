using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class MemorySessionTests : AbstractSessionTests
    {
        protected override IEditableSession GetSession()
        {
            return new MemorySession(IPAddress.Loopback, DateTime.Now);
        }
    }
}
