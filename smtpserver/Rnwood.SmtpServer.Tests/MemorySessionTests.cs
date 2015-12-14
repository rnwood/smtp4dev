using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;

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