using MailKit.Net.Smtp;
using MimeKit;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.SmtpServer.Tests
{
    public class ClientTests
    {
        [Fact]
        public void SmtpClient_NonSSL()
        {
            using (DefaultServer server = new DefaultServer(Ports.AssignAutomatically))
            {
                IMessage receivedMessage = null;
                server.MessageReceived += (o, ea) =>
                {
                    receivedMessage = ea.Message;
                };
                server.Start();

                MimeMessage message = new MimeMessage();
                message.From.Add(new MailboxAddress("", "from@from.com"));
                message.To.Add(new MailboxAddress("", "to@to.com"));
                message.Subject = "subject";
                message.Body = new TextPart("plain")
                {
                    Text = "body"
                };

                using (SmtpClient client = new SmtpClient())
                {
                    client.Connect("localhost", server.PortNumber);

                    client.Send(message);
                }

                Assert.NotNull(receivedMessage);
                Assert.Equal("from@from.com", receivedMessage.From);
            }
        }
    }
}