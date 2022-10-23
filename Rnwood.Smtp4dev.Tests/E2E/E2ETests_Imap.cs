using LumiSoft.Net.Mime;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using MailboxAddress = MimeKit.MailboxAddress;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public class E2ETests_Imap : E2ETests
    {
        public E2ETests_Imap(ITestOutputHelper output) : base(output)
        {


        }

        [Fact]
        public void MessagesAvailable()
        {
            RunE2ETest(context => {


                string messageSubject = Guid.NewGuid().ToString();
                using (SmtpClient smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.CheckCertificateRevocation = false;
                    MimeMessage message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("to@to.com"));
                    message.From.Add(MailboxAddress.Parse("from@from.com"));

                    message.Subject = messageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "Body of end to end test"
                    };

                    smtpClient.Connect("localhost", context.SmtpPortNumber, SecureSocketOptions.StartTls, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }


                using (ImapClient imapClient = new ImapClient())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");
                    imapClient.Inbox.Open(MailKit.FolderAccess.ReadWrite);

                    var imapMessageSummary = imapClient.Inbox.Fetch(0, 0, MessageSummaryItems.UniqueId|MessageSummaryItems.Full);
                    Assert.Equal(messageSubject, imapMessageSummary[0].NormalizedSubject);
                    var imapMessage = imapClient.Inbox.GetMessage(imapMessageSummary[0].UniqueId);
                    Assert.NotNull(imapMessage);
                    Assert.Equal(messageSubject, imapMessage.Subject);

                    imapClient.Inbox.AddFlags(imapMessageSummary[0].UniqueId, MessageFlags.Seen, true);
                    imapClient.Inbox.Close();
                }

                using (ImapClient imapClient = new ImapClient())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");
                    imapClient.Inbox.Open(MailKit.FolderAccess.ReadWrite);

                    var imapMessageSummary = imapClient.Inbox.Fetch(0, 0, MessageSummaryItems.All);
                    Assert.True(imapMessageSummary[0].Flags.Value.HasFlag(MessageFlags.Seen));
                    imapClient.Inbox.Close();
                }

            });
        }
    }
}
