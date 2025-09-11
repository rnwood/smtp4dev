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
                // We focus on verifying the APPEND succeeds without exceptions
                
                using (var imapClient = new ImapClient())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");
                    
                    // Create a test message
                    var message = new MimeMessage();
                    message.To.Add(MailboxAddress.Parse("recipient@example.com"));
                    message.From.Add(MailboxAddress.Parse("sender@example.com"));
                    message.Subject = "Test APPEND NRE Fix " + Guid.NewGuid();
                    message.Body = new TextPart()
                    {
                        Text = "This tests the NRE fix for APPEND to Sent folder."
                    };
                    
                    // Get the Sent folder - this should work without NullReferenceException
                    var sentFolder = imapClient.GetFolder("Sent");
                    
                    // This should not throw NullReferenceException and should return a valid UID
                    var appendResult = sentFolder.Append(message, MessageFlags.Seen);
                    
                    // The key test: APPEND should complete without NullReferenceException
                    // We don't need to verify the message is retrievable since that's tested elsewhere
                    Assert.NotNull(appendResult);
                    Assert.True(appendResult.Value.IsValid);
                    
                    imapClient.Disconnect(true);
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
                    
                    imapClient.Disconnect(true);
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
                    
                    imapClient.Disconnect(true);
                }
            });
        }
    }
}