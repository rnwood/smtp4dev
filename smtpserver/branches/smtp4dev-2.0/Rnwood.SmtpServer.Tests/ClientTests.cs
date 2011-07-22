using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using MbUnit.Framework;

namespace Rnwood.SmtpServer.Tests
{
    [TestFixture]
    public class ClientTests
    {

        [Test]
        public void SmtpClient_NonSSL()
        {
            DefaultServer server = new DefaultServer(Ports.AssignAutomatically);
            server.Start();

            SmtpClient client = new SmtpClient("localhost", server.PortNumber);
            client.Send("from@from.com", "to@to.com", "subject", "body");

            server.Stop();

            
        }
    }
}
