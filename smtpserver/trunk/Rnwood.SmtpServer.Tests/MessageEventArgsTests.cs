using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class MessageEventArgsTests
    {
        [Test]
        public void Message()
        {
            Mocks mocks = new Mocks();

            MessageEventArgs messageEventArgs = new MessageEventArgs(mocks.Message.Object);
            
            Assert.AreSame(mocks.Message.Object, messageEventArgs.Message);
        }
    }
}
