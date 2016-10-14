using Xunit;
using System;
using System.Net;

namespace Rnwood.SmtpServer.Tests
{
    
    public class MemorySessionTests : AbstractSessionTests
    {
        protected override IEditableSession GetSession()
        {
            return new MemorySession(IPAddress.Loopback, DateTime.Now);
        }
    }
}