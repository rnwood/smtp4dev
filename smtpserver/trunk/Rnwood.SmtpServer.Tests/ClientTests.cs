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
        [Timeout(10)]
        [Disable]
        public void SmtpClient_NonSSL()
        {
            DefaultServer server = new DefaultServer(Ports.AssignAutomatically);
            //server.Behaviour.ReceiveTimeout = TimeSpan.FromMilliseconds(500);
            
            int portNumber = server.Start();

            try
            {
                SmtpClient client = new SmtpClient("localhost", portNumber);
                client.Send("from@from.com", "to@to.com", "subject", "body");
            }
            finally
            {
                server.Stop(ServerStopBehaviour.KillExistingConnections);
            }
        }
    }
}
