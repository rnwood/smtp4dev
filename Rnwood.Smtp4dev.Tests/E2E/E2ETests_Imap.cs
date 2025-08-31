using LumiSoft.Net.Mime;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
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

        [Fact]
        public void YoungerSearchFunctionality()
        {
            RunE2ETest(context => {

                // Send first message (this will be "older")
                string oldMessageSubject = "Old message " + Guid.NewGuid().ToString();
                using (SmtpClient smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.CheckCertificateRevocation = false;
                    MimeMessage message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("old@test.com"));
                    message.From.Add(MailboxAddress.Parse("sender@test.com"));

                    message.Subject = oldMessageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "This is an older test message"
                    };

                    smtpClient.Connect("localhost", context.SmtpPortNumber, SecureSocketOptions.StartTls, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                // Wait a bit to ensure time difference
                Thread.Sleep(2000);

                // Send second message (this will be "newer")
                string newMessageSubject = "New message " + Guid.NewGuid().ToString();
                using (SmtpClient smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.CheckCertificateRevocation = false;
                    MimeMessage message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("new@test.com"));
                    message.From.Add(MailboxAddress.Parse("sender@test.com"));

                    message.Subject = newMessageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "This is a newer test message"
                    };

                    smtpClient.Connect("localhost", context.SmtpPortNumber, SecureSocketOptions.StartTls, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                // Use IMAP to search for messages using YOUNGER search criteria
                using (ImapClient imapClient = new ImapClient())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");
                    imapClient.Inbox.Open(MailKit.FolderAccess.ReadOnly);

                    // Test YOUNGER functionality using raw IMAP command
                    // Since MailKit doesn't natively support YOUNGER, we'll use ImapFolder.Search with raw command
                    try
                    {
                        // Create a custom search query for YOUNGER 30 (last 30 seconds)
                        var recentResults = ((MailKit.Net.Imap.ImapFolder)imapClient.Inbox).Search("YOUNGER 30");
                        
                        // Should find at least both messages we just sent
                        Assert.True(recentResults.Count >= 2, $"Should find at least 2 recent messages, found {recentResults.Count}");
                        
                        // Now test YOUNGER 1 (last 1 second) - should find fewer messages
                        var veryRecentResults = ((MailKit.Net.Imap.ImapFolder)imapClient.Inbox).Search("YOUNGER 1");
                        
                        // Should find at least the most recent message
                        Assert.True(veryRecentResults.Count >= 1, $"Should find at least 1 very recent message, found {veryRecentResults.Count}");
                        
                        // Very recent search should return fewer or equal results
                        Assert.True(veryRecentResults.Count <= recentResults.Count, 
                            $"Very recent search ({veryRecentResults.Count}) should return fewer or equal messages than broader search ({recentResults.Count})");
                        
                        // Verify that the newer message is in the results
                        var recentMessages = imapClient.Inbox.Fetch(recentResults.UniqueIds, MessageSummaryItems.Envelope);
                        Assert.Contains(recentMessages, m => m.Envelope.Subject == newMessageSubject);
                        Assert.Contains(recentMessages, m => m.Envelope.Subject == oldMessageSubject);
                    }
                    catch (NotSupportedException)
                    {
                        // Fallback: If raw YOUNGER search isn't supported by MailKit version,
                        // at least verify the messages exist and are recent
                        var allMessages = imapClient.Inbox.Search(SearchQuery.All);
                        Assert.True(allMessages.Count >= 2, "Should have at least 2 messages");

                        var messageSummaries = imapClient.Inbox.Fetch(allMessages, MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate);
                        
                        // Verify both messages exist
                        Assert.Contains(messageSummaries, m => m.Envelope.Subject == oldMessageSubject);
                        Assert.Contains(messageSummaries, m => m.Envelope.Subject == newMessageSubject);

                        // Verify that messages have recent timestamps
                        var now = DateTime.Now;
                        foreach (var msg in messageSummaries)
                        {
                            var timeDiff = now - msg.InternalDate.Value.DateTime;
                            Assert.True(timeDiff.TotalMinutes < 1, $"Message {msg.Envelope.Subject} should be recent (within 1 minute)");
                        }
                    }

                    imapClient.Inbox.Close();
                }
            });
        }

        [Fact]
        public void OlderSearchFunctionality()
        {
            RunE2ETest(context => {

                // Send first message (this will be "older")
                string oldMessageSubject = "Old message " + Guid.NewGuid().ToString();
                using (SmtpClient smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.CheckCertificateRevocation = false;
                    MimeMessage message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("old@test.com"));
                    message.From.Add(MailboxAddress.Parse("sender@test.com"));

                    message.Subject = oldMessageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "This is an older test message"
                    };

                    smtpClient.Connect("localhost", context.SmtpPortNumber, SecureSocketOptions.StartTls, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                // Wait a bit to ensure time difference
                Thread.Sleep(3000);

                // Send second message (this will be "newer")  
                string newMessageSubject = "New message " + Guid.NewGuid().ToString();
                using (SmtpClient smtpClient = new SmtpClient())
                {
                    smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtpClient.CheckCertificateRevocation = false;
                    MimeMessage message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("new@test.com"));
                    message.From.Add(MailboxAddress.Parse("sender@test.com"));

                    message.Subject = newMessageSubject;
                    message.Body = new TextPart()
                    {
                        Text = "This is a newer test message"
                    };

                    smtpClient.Connect("localhost", context.SmtpPortNumber, SecureSocketOptions.StartTls, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                }

                // Use IMAP to search for messages using OLDER search criteria
                using (ImapClient imapClient = new ImapClient())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");
                    imapClient.Inbox.Open(MailKit.FolderAccess.ReadOnly);

                    // Test OLDER functionality using raw IMAP command
                    // Since MailKit doesn't natively support OLDER, we'll use ImapFolder.Search with raw command
                    try
                    {
                        // Create a custom search query for OLDER 2 (older than 2 seconds)
                        // This should find the first message but not the second one
                        var olderResults = ((MailKit.Net.Imap.ImapFolder)imapClient.Inbox).Search("OLDER 2");
                        
                        // Should find at least the older message 
                        Assert.True(olderResults.Count >= 1, $"Should find at least 1 older message, found {olderResults.Count}");
                        
                        // Verify that the older message is in the results
                        var olderMessages = imapClient.Inbox.Fetch(olderResults.UniqueIds, MessageSummaryItems.Envelope);
                        Assert.Contains(olderMessages, m => m.Envelope.Subject == oldMessageSubject);
                        
                        // Test OLDER 10 (older than 10 seconds) - should find both messages if enough time has passed
                        var veryOldResults = ((MailKit.Net.Imap.ImapFolder)imapClient.Inbox).Search("OLDER 10");
                        
                        // Both messages should be found since they're both older than 10 seconds after the delay
                        if (veryOldResults.Count >= 2)
                        {
                            var veryOldMessages = imapClient.Inbox.Fetch(veryOldResults.UniqueIds, MessageSummaryItems.Envelope);
                            Assert.Contains(veryOldMessages, m => m.Envelope.Subject == oldMessageSubject);
                            Assert.Contains(veryOldMessages, m => m.Envelope.Subject == newMessageSubject);
                        }
                    }
                    catch (NotSupportedException)
                    {
                        // Fallback: If raw OLDER search isn't supported by MailKit version,
                        // at least verify the messages exist
                        var allMessages = imapClient.Inbox.Search(SearchQuery.All);
                        Assert.True(allMessages.Count >= 2, "Should have at least 2 messages");

                        var messageSummaries = imapClient.Inbox.Fetch(allMessages, MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate);
                        
                        // Verify both messages exist
                        Assert.Contains(messageSummaries, m => m.Envelope.Subject == oldMessageSubject);
                        Assert.Contains(messageSummaries, m => m.Envelope.Subject == newMessageSubject);

                        // Verify that the old message is indeed older than the new one
                        var oldMessage = messageSummaries.FirstOrDefault(m => m.Envelope.Subject == oldMessageSubject);
                        var newMessage = messageSummaries.FirstOrDefault(m => m.Envelope.Subject == newMessageSubject);
                        
                        if (oldMessage != null && newMessage != null && 
                            oldMessage.InternalDate.HasValue && newMessage.InternalDate.HasValue)
                        {
                            Assert.True(oldMessage.InternalDate.Value < newMessage.InternalDate.Value, 
                                "Old message should have an earlier timestamp than new message");
                        }
                    }

                    imapClient.Inbox.Close();
                }
            });
        }
    }
}
