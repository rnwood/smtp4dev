using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.Imap
{
    [Collection("E2ETests")]
    public class E2ETests_ImapFolders : E2ETests
    {
        public E2ETests_ImapFolders(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CreateFolderAndMoveMessageIntoIt()
        {
            RunE2ETest(context =>
            {
                string messageSubject = "Folder test " + Guid.NewGuid();

                using (ImapClient imapClient = CreateImapClientWithLogging())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");

                    imapClient.Inbox.Append(CreateMessage(messageSubject), MessageFlags.None);

                    var root = imapClient.GetFolder(imapClient.PersonalNamespaces[0]);
                    var doneFolder = root.Create("DONE", true);
                    Assert.Contains(root.GetSubfolders(false), f => f.FullName == "DONE");

                    // Creating an existing folder must fail
                    Assert.Throws<ImapCommandException>(() => root.Create("DONE", true));

                    // Move using COPY + STORE \Deleted + EXPUNGE
                    imapClient.Inbox.Open(FolderAccess.ReadWrite);
                    var inboxMessage = imapClient.Inbox.Fetch(0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope)
                        .Single(m => m.NormalizedSubject == messageSubject);
                    imapClient.Inbox.CopyTo(inboxMessage.UniqueId, doneFolder);
                    imapClient.Inbox.AddFlags(inboxMessage.UniqueId, MessageFlags.Deleted, true);
                    imapClient.Inbox.Expunge();
                    imapClient.Inbox.Close();

                    imapClient.Inbox.Open(FolderAccess.ReadOnly);
                    Assert.Equal(0, imapClient.Inbox.Count);
                    imapClient.Inbox.Close();

                    doneFolder.Open(FolderAccess.ReadWrite);
                    Assert.Equal(1, doneFolder.Count);
                    var doneMessage = doneFolder.GetMessage(0);
                    Assert.Equal(messageSubject, doneMessage.Subject);
                    doneFolder.Close();

                    imapClient.Disconnect(true);
                }
            });
        }

        [Fact]
        public void AppendToCreatedFolder()
        {
            RunE2ETest(context =>
            {
                using (ImapClient imapClient = CreateImapClientWithLogging())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");

                    var root = imapClient.GetFolder(imapClient.PersonalNamespaces[0]);
                    var archiveFolder = root.Create("Archive", true);
                    archiveFolder.Append(CreateMessage("Appended " + Guid.NewGuid()), MessageFlags.Seen);

                    archiveFolder.Open(FolderAccess.ReadOnly);
                    Assert.Equal(1, archiveFolder.Count);
                    archiveFolder.Close();

                    imapClient.Disconnect(true);
                }
            });
        }

        [Fact]
        public void CreateSubfolder()
        {
            RunE2ETest(context =>
            {
                using (ImapClient imapClient = CreateImapClientWithLogging())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");

                    var root = imapClient.GetFolder(imapClient.PersonalNamespaces[0]);
                    root.Create("Processed", true).Create("2024", true);

                    var processed = root.GetSubfolders(false).Single(f => f.FullName == "Processed");
                    Assert.Contains(processed.GetSubfolders(false), f => f.FullName == "Processed/2024");

                    imapClient.Disconnect(true);
                }
            });
        }

        [Fact]
        public void RenameAndDeleteFolder()
        {
            RunE2ETest(context =>
            {
                using (ImapClient imapClient = CreateImapClientWithLogging())
                {
                    imapClient.Connect("localhost", context.ImapPortNumber);
                    imapClient.Authenticate("user", "password");

                    var root = imapClient.GetFolder(imapClient.PersonalNamespaces[0]);
                    var folder = root.Create("ToRename", true);
                    folder.Rename(root, "Renamed");

                    var names = root.GetSubfolders(false).Select(f => f.FullName).ToList();
                    Assert.Contains("Renamed", names);
                    Assert.DoesNotContain("ToRename", names);

                    root.GetSubfolders(false).Single(f => f.FullName == "Renamed").Delete();
                    Assert.DoesNotContain(root.GetSubfolders(false), f => f.FullName == "Renamed");

                    // Default folders cannot be deleted
                    Assert.Throws<ImapCommandException>(() => root.GetSubfolder("Sent").Delete());

                    imapClient.Disconnect(true);
                }
            });
        }

        private static MimeMessage CreateMessage(string subject)
        {
            var message = new MimeMessage();
            message.To.Add(MailboxAddress.Parse("recipient@example.com"));
            message.From.Add(MailboxAddress.Parse("sender@example.com"));
            message.Subject = subject;
            message.Body = new TextPart() { Text = "Body of folder test" };
            return message;
        }
    }
}
