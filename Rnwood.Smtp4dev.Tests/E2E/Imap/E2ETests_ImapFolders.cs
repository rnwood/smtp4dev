using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public void AppendToFolderDeletedDuringLiteralReturnsTryCreate()
        {
            RunE2ETest(context =>
            {
                using var deletingClient = CreateImapClientWithLogging();
                deletingClient.Connect("localhost", context.ImapPortNumber);
                deletingClient.Authenticate("user", "password");
                var root = deletingClient.GetFolder(deletingClient.PersonalNamespaces[0]);
                root.Create("AppendRace", true);

                using var client = new TcpClient();
                client.ReceiveTimeout = 10000;
                client.Connect("localhost", context.ImapPortNumber);
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, true)
                {
                    AutoFlush = true,
                    NewLine = "\r\n"
                };

                Assert.StartsWith("* OK", reader.ReadLine());
                writer.WriteLine("a1 LOGIN user password");
                ReadTaggedResponse(reader, "a1");

                using var messageStream = new MemoryStream();
                CreateMessage("Append race " + Guid.NewGuid()).WriteTo(messageStream);
                var messageData = messageStream.ToArray();

                writer.WriteLine($"a2 APPEND \"AppendRace\" {{{messageData.Length}}}");
                Assert.StartsWith("+", reader.ReadLine());

                root.GetSubfolder("AppendRace").Delete();

                stream.Write(messageData);
                stream.Write(new byte[] { (byte)'\r', (byte)'\n' });
                stream.Flush();

                var appendResponse = ReadTaggedResponse(reader, "a2");
                Assert.Contains("a2 NO [TRYCREATE]", appendResponse);

                writer.WriteLine("a3 LOGOUT");
                ReadTaggedResponse(reader, "a3");
                deletingClient.Disconnect(true);
            });
        }

        [Fact]
        public void EmptyListAndLSubPatternsReturnTheHierarchyRoot()
        {
            RunE2ETest(context =>
            {
                using var client = new TcpClient();
                client.Connect("localhost", context.ImapPortNumber);
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, true)
                {
                    AutoFlush = true,
                    NewLine = "\r\n"
                };

                Assert.StartsWith("* OK", reader.ReadLine());

                writer.WriteLine("a1 LOGIN user password");
                ReadTaggedResponse(reader, "a1");

                writer.WriteLine("a2 LIST \"\" \"\"");
                Assert.Contains("* LIST (\\Noselect) \"/\" \"\"", ReadTaggedResponse(reader, "a2"));

                writer.WriteLine("a3 LSUB \"\" \"\"");
                Assert.Contains("* LSUB (\\Noselect) \"/\" \"\"", ReadTaggedResponse(reader, "a3"));

                writer.WriteLine("a4 CREATE Archive");
                ReadTaggedResponse(reader, "a4");

                writer.WriteLine("a5 LSUB \"Archive/\" \"\"");
                var referenceRootResponse = ReadTaggedResponse(reader, "a5");
                Assert.Contains("* LSUB (\\Noselect) \"/\"", referenceRootResponse);
                Assert.Contains("Archive/", referenceRootResponse);

                writer.WriteLine("a6 LOGOUT");
                ReadTaggedResponse(reader, "a6");
            });
        }

        [Fact]
        public void ConcurrentCreateFolderOnlySucceedsOnce()
        {
            RunE2ETest(context =>
            {
                using var firstClient = CreateImapClientWithLogging();
                using var secondClient = CreateImapClientWithLogging();
                firstClient.Connect("localhost", context.ImapPortNumber);
                firstClient.Authenticate("user", "password");
                secondClient.Connect("localhost", context.ImapPortNumber);
                secondClient.Authenticate("user", "password");

                var roots = new[]
                {
                    firstClient.GetFolder(firstClient.PersonalNamespaces[0]),
                    secondClient.GetFolder(secondClient.PersonalNamespaces[0])
                };
                using var start = new Barrier(roots.Length);
                var createResults = roots.Select(root => Task.Factory.StartNew(() =>
                {
                    if (!start.SignalAndWait(TimeSpan.FromSeconds(30)))
                    {
                        throw new TimeoutException("Timed out waiting to start concurrent folder creation.");
                    }

                    try
                    {
                        root.Create("Concurrent", true);
                        return true;
                    }
                    catch (ImapCommandException)
                    {
                        return false;
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

                var results = Task.WhenAll(createResults).GetAwaiter().GetResult();

                Assert.Single(results, created => created);
                Assert.Single(results, created => !created);
                Assert.Single(roots[0].GetSubfolders(false), folder => folder.FullName == "Concurrent");

                firstClient.Disconnect(true);
                secondClient.Disconnect(true);
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

                    var selectedFolder = root.Create("Selected", true);
                    selectedFolder.Open(FolderAccess.ReadWrite);
                    Assert.Throws<ImapCommandException>(() => selectedFolder.Delete());
                    Assert.Contains(root.GetSubfolders(false), f => f.FullName == "Selected");

                    imapClient.Disconnect(true);
                }
            });
        }

        [Fact]
        public void CannotRenameParentWhileAnotherSessionHasDescendantSelected()
        {
            RunE2ETest(context =>
            {
                using var selectedClient = CreateImapClientWithLogging();
                using var renamingClient = CreateImapClientWithLogging();
                selectedClient.Connect("localhost", context.ImapPortNumber);
                selectedClient.Authenticate("user", "password");
                renamingClient.Connect("localhost", context.ImapPortNumber);
                renamingClient.Authenticate("user", "password");

                var selectedRoot = selectedClient.GetFolder(selectedClient.PersonalNamespaces[0]);
                selectedRoot.Create("Parent", true).Create("Child", true);
                var selectedChild = selectedRoot.GetSubfolder("Parent").GetSubfolder("Child");
                selectedChild.Open(FolderAccess.ReadOnly);

                var renamingRoot = renamingClient.GetFolder(renamingClient.PersonalNamespaces[0]);
                var parent = renamingRoot.GetSubfolder("Parent");
                Assert.Throws<ImapCommandException>(() => parent.Rename(renamingRoot, "RenamedParent"));
                Assert.Contains(renamingRoot.GetSubfolders(false), folder => folder.FullName == "Parent");
                Assert.DoesNotContain(renamingRoot.GetSubfolders(false), folder => folder.FullName == "RenamedParent");

                selectedClient.Disconnect(true);
                renamingClient.Disconnect(true);
            });
        }

        [Fact]
        public void UidNextUsesTheGlobalAllocatorForAnEmptyFolder()
        {
            RunE2ETest(context =>
            {
                using var imapClient = CreateImapClientWithLogging();
                imapClient.Connect("localhost", context.ImapPortNumber);
                imapClient.Authenticate("user", "password");

                imapClient.Inbox.Append(CreateMessage("UID seed " + Guid.NewGuid()), MessageFlags.None);

                var root = imapClient.GetFolder(imapClient.PersonalNamespaces[0]);
                var archiveFolder = root.Create("Archive", true);
                imapClient.Inbox.Open(FolderAccess.ReadOnly);
                var expectedUidNext = imapClient.Inbox.UidNext;
                imapClient.Inbox.Close();

                archiveFolder.Open(FolderAccess.ReadOnly);
                var uidNext = archiveFolder.UidNext;

                Assert.True(uidNext.HasValue);
                Assert.Equal(expectedUidNext, uidNext);

                archiveFolder.Close();
                imapClient.Disconnect(true);
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

        private static string ReadTaggedResponse(StreamReader reader, string tag)
        {
            var lines = new List<string>();
            string line;
            do
            {
                line = reader.ReadLine();
                if (line == null)
                {
                    throw new EndOfStreamException($"The IMAP server closed the connection before responding to {tag}.");
                }

                lines.Add(line);
            }
            while (!line.StartsWith(tag + " ", StringComparison.Ordinal));

            return string.Join(Environment.NewLine, lines);
        }
    }
}
