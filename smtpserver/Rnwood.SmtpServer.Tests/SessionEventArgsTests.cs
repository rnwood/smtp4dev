using Xunit;

namespace Rnwood.SmtpServer.Tests
{
    
    public class SessionEventArgsTests
    {
        [Fact]
        public void Session()
        {
            Mocks mocks = new Mocks();

            SessionEventArgs s = new SessionEventArgs(mocks.Session.Object);

            Assert.Equal(s.Session, mocks.Session.Object);
        }
    }
}