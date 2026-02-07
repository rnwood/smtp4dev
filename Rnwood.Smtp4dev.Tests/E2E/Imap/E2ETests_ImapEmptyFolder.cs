using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.Imap
{
    [Collection("E2ETests")]
    public class E2ETests_Imap_EmptyFolder : E2ETests
    {
        public E2ETests_Imap_EmptyFolder(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void SelectEmptyInboxDoesNotThrowError()
        {
            RunE2ETest(context => {
                // Connect to IMAP server with no messages
                // Note: "user"/"password" are test-only credentials configured for E2E tests
                using (ImapClient imapClient = CreateImapClientWithLogging())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");
                    
                    // This should not throw an error even if the folder is empty
                    imapClient.Inbox.Open(MailKit.FolderAccess.ReadWrite);
                    
                    // Verify folder is empty
                    Assert.Equal(0, imapClient.Inbox.Count);
                    
                    imapClient.Inbox.Close();
                }
            });
        }

        [Fact]
        public void SelectEmptySentFolderDoesNotThrowError()
        {
            RunE2ETest(context => {
                // Connect to IMAP server with no messages
                // Note: "user"/"password" are test-only credentials configured for E2E tests
                using (ImapClient imapClient = CreateImapClientWithLogging())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");
                    
                    // Get the Sent folder
                    var sentFolder = imapClient.GetFolder(imapClient.PersonalNamespaces[0]).GetSubfolder("Sent");
                    
                    // This should not throw an error even if the folder is empty
                    sentFolder.Open(MailKit.FolderAccess.ReadWrite);
                    
                    // Verify folder is empty
                    Assert.Equal(0, sentFolder.Count);
                    
                    sentFolder.Close();
                }
            });
        }
    }
}
