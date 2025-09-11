using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;
using System;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
    public class E2ETests_ImapAppend : E2ETests
    {
        public E2ETests_ImapAppend(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void AppendToSentFolder_ShouldNotThrowNullReference()
        {
            RunE2ETest(context => 
            {
                // This test verifies that IMAP APPEND to Sent folder does not throw NullReferenceException
                // We use a direct protocol test rather than MailKit to avoid client-specific issues
                
                using (var client = new System.Net.Sockets.TcpClient("localhost", context.ImapPortNumber))
                using (var stream = client.GetStream())
                using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                using (var writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8) { AutoFlush = true })
                {
                    // Read greeting
                    var greeting = reader.ReadLine();
                    Assert.StartsWith("* OK", greeting);
                    
                    // Login
                    writer.WriteLine("A001 LOGIN user password");
                    var loginResponse = reader.ReadLine();
                    Assert.Equal("A001 OK LOGIN completed.", loginResponse);
                    
                    // Prepare test message
                    var testMessage = $"From: sender@example.com\r\nTo: recipient@example.com\r\nSubject: Test APPEND NRE Fix {Guid.NewGuid()}\r\n\r\nThis tests the NRE fix.\r\n";
                    
                    // Send APPEND command to Sent folder
                    writer.WriteLine($"A002 APPEND Sent (\\Seen) {{{testMessage.Length}}}");
                    
                    // Should get continuation response (not a server error)
                    var continuation = reader.ReadLine();
                    Assert.Equal("+ Ready for literal data.", continuation);
                    
                    // Send message data
                    writer.Write(testMessage);
                    writer.WriteLine(); // CRLF to terminate
                    
                    // Should get success response (indicates no NRE occurred)
                    var appendResponse = reader.ReadLine();
                    Assert.StartsWith("A002 OK APPEND completed", appendResponse);
                    
                    // Logout
                    writer.WriteLine("A003 LOGOUT");
                    var logoutResponse = reader.ReadLine();
                    Assert.StartsWith("* BYE", logoutResponse);
                }
            });
        }

        [Fact]
        public void AppendToInboxFolder_ShouldWork()
        {
            RunE2ETest(context => 
            {
                // Create a test message
                var message = new MimeMessage();
                message.To.Add(MailboxAddress.Parse("recipient@example.com"));
                message.From.Add(MailboxAddress.Parse("sender@example.com"));
                message.Subject = "Test message for INBOX folder - " + Guid.NewGuid().ToString();
                message.Body = new TextPart()
                {
                    Text = "This is a test message that should be appended to the INBOX folder"
                };

                // Connect to IMAP server and try to append to INBOX folder
                using (var imapClient = new ImapClient())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");

                    // Get the INBOX folder
                    var inboxFolder = imapClient.Inbox;
                    
                    // This should work without issues
                    var appendResult = inboxFolder.Append(message, MessageFlags.None);
                    
                    // Verify the message was appended successfully
                    Assert.NotNull(appendResult);
                    Assert.True(appendResult.Value.IsValid);
                    
                    // Verify we can retrieve the message from INBOX
                    inboxFolder.Open(FolderAccess.ReadOnly);
                    var messages = inboxFolder.Fetch(0, -1, MessageSummaryItems.Envelope | MessageSummaryItems.Flags);
                    
                    Assert.NotEmpty(messages);
                    Assert.Contains(messages, m => m.Envelope.Subject == message.Subject);
                    
                    inboxFolder.Close();
                }
            });
        }

        [Fact]
        public void AppendToUnsupportedFolder_ShouldFail()
        {
            RunE2ETest(context => 
            {
                // Create a test message
                var message = new MimeMessage();
                message.To.Add(MailboxAddress.Parse("recipient@example.com"));
                message.From.Add(MailboxAddress.Parse("sender@example.com"));
                message.Subject = "Test message for unsupported folder - " + Guid.NewGuid().ToString();
                message.Body = new TextPart()
                {
                    Text = "This message should fail to append to an unsupported folder"
                };

                // Connect to IMAP server and try to append to an unsupported folder
                using (var imapClient = new ImapClient())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");

                    // Try to get a folder that doesn't exist/isn't supported
                    Assert.Throws<FolderNotFoundException>(() => 
                    {
                        var unsupportedFolder = imapClient.GetFolder("Drafts");
                        unsupportedFolder.Append(message, MessageFlags.None);
                    });
                }
            });
        }
    }
}