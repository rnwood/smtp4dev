using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rnwood.SmtpServer.Tests
{
    [TestClass]
    public class MessageEventArgsTests
    {
        [TestMethod]
        public void Message()
        {
            Mocks mocks = new Mocks();

            MessageEventArgs messageEventArgs = new MessageEventArgs(mocks.Message.Object);
            
            Assert.AreSame(mocks.Message.Object, messageEventArgs.Message);
        }
    }
}
