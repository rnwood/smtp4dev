using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Server;
using LumiSoft.Net.Mail;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rnwood.Smtp4dev.Data;
using Serilog;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Server.Imap;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;

namespace Rnwood.Smtp4dev.Server
{
    public partial class ImapServer
    {
        class SessionHandler
        {
            private readonly ILogger log = Log.ForContext<SessionHandler>();
            public SessionHandler(IMAP_Session session, ScriptingHost scriptingHost, IOptionsMonitor<ServerOptions> serverOptions, IServiceScopeFactory serviceScopeFactory)
            {
                this.session = session;
                session.Create += Session_Create;
                session.Delete += Session_Delete;
                session.Rename += Session_Rename;
                session.Copy += Session_Copy;
                session.Append += Session_Append;
                session.List += Session_List;
                session.LSub += Session_LSub;
                session.Login += Session_Login;
                session.Fetch += Session_Fetch;
                session.GetMessagesInfo += Session_GetMessagesInfo;
                session.Capabilities.Remove("QUOTA");
                session.Capabilities.Remove("NAMESPACE");
                session.Capabilities.Remove("UTF8=ACCEPT");
                session.Store += Session_Store;
                session.Select += Session_Select;
                session.Search += Session_Search;
                this.scriptingHost = scriptingHost;
                this.serverOptions = serverOptions;
                this.serviceScopeFactory = serviceScopeFactory;
            }


            private void Session_Create(object sender, IMAP_e_Folder e)
            {
                var folderName = ImapFolderNames.Normalize(e.Folder);
                if (string.IsNullOrEmpty(folderName))
                {
                    e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", "Invalid folder name");
                    return;
                }

                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                    var dbContext = messagesRepository.DbContext;
                    var mailbox = GetMailbox(dbContext);

                    if (mailbox.MailboxFolders.Any(f => f.Name == folderName))
                    {
                        e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", $"Folder '{folderName}' already exists");
                        return;
                    }

                    // Create any superior hierarchical names that are needed (RFC 3501 6.3.3)
                    foreach (var name in ImapFolderNames.GetAncestors(folderName).Append(folderName)
                        .Where(n => !mailbox.MailboxFolders.Any(f => f.Name == n)))
                    {
                        log.Information("Creating folder {folder} in mailbox {mailboxName}", name, mailbox.Name);
                        mailbox.MailboxFolders.Add(new MailboxFolder
                        {
                            Name = name,
                            MailboxId = mailbox.Id,
                            Mailbox = mailbox
                        });
                    }

                    dbContext.SaveChanges();
                    NotifyMessagesChanged(scope, mailbox.Name);
                }
            }

            private void Session_Delete(object sender, IMAP_e_Folder e)
            {
                var folderName = ImapFolderNames.Normalize(e.Folder);
                if (ImapFolderNames.IsDefaultFolder(folderName))
                {
                    e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", $"Folder '{folderName}' cannot be deleted");
                    return;
                }

                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                    var dbContext = messagesRepository.DbContext;
                    var mailbox = GetMailbox(dbContext);

                    var folder = mailbox.MailboxFolders.FirstOrDefault(f => f.Name == folderName);
                    if (folder == null)
                    {
                        e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", $"Folder '{folderName}' does not exist");
                        return;
                    }

                    if (mailbox.MailboxFolders.Any(f => ImapFolderNames.IsDescendantOf(f.Name, folderName)))
                    {
                        e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", $"Folder '{folderName}' has inferior hierarchical names");
                        return;
                    }

                    log.Information("Deleting folder {folder} in mailbox {mailboxName}", folderName, mailbox.Name);
                    dbContext.Messages.RemoveRange(dbContext.Messages.Where(m => m.MailboxFolderId == folder.Id));
                    dbContext.MailboxFolders.Remove(folder);
                    dbContext.SaveChanges();
                    NotifyMessagesChanged(scope, mailbox.Name);
                }
            }

            private void Session_Rename(object sender, IMAP_e_Rename e)
            {
                var currentName = ImapFolderNames.Normalize(e.CurrentFolder);
                var newName = ImapFolderNames.Normalize(e.NewFolder);

                if (ImapFolderNames.IsDefaultFolder(currentName))
                {
                    e.Response = new IMAP_r_ServerStatus(e.CmdTag, "NO", $"Folder '{currentName}' cannot be renamed");
                    return;
                }

                if (string.IsNullOrEmpty(newName) || newName == currentName || ImapFolderNames.IsDescendantOf(newName, currentName))
                {
                    e.Response = new IMAP_r_ServerStatus(e.CmdTag, "NO", "Invalid new folder name");
                    return;
                }

                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                    var dbContext = messagesRepository.DbContext;
                    var mailbox = GetMailbox(dbContext);

                    var folder = mailbox.MailboxFolders.FirstOrDefault(f => f.Name == currentName);
                    if (folder == null)
                    {
                        e.Response = new IMAP_r_ServerStatus(e.CmdTag, "NO", $"Folder '{currentName}' does not exist");
                        return;
                    }

                    if (mailbox.MailboxFolders.Any(f => f.Name == newName))
                    {
                        e.Response = new IMAP_r_ServerStatus(e.CmdTag, "NO", $"Folder '{newName}' already exists");
                        return;
                    }

                    log.Information("Renaming folder {folder} to {newFolder} in mailbox {mailboxName}", currentName, newName, mailbox.Name);

                    // Create superior hierarchical names
                    foreach (var ancestorName in ImapFolderNames.GetAncestors(newName)
                        .Where(n => !mailbox.MailboxFolders.Any(f => f.Name == n)))
                    {
                        mailbox.MailboxFolders.Add(new MailboxFolder
                        {
                            Name = ancestorName,
                            MailboxId = mailbox.Id,
                            Mailbox = mailbox
                        });
                    }

                    // Inferior hierarchical names must also be renamed (RFC 3501 6.3.5)
                    foreach (var descendant in mailbox.MailboxFolders.Where(f => ImapFolderNames.IsDescendantOf(f.Name, currentName)))
                    {
                        descendant.Name = newName + descendant.Name.Substring(currentName.Length);
                    }
                    folder.Name = newName;

                    dbContext.SaveChanges();
                    NotifyMessagesChanged(scope, mailbox.Name);
                }

                e.Response = new IMAP_r_ServerStatus(e.CmdTag, "OK", "RENAME completed");
            }

            private void Session_Copy(object sender, IMAP_e_Copy e)
            {
                var targetFolderName = ImapFolderNames.Normalize(e.TargetFolder);

                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                    var dbContext = messagesRepository.DbContext;
                    var mailbox = GetMailbox(dbContext);

                    var targetFolder = mailbox.MailboxFolders.FirstOrDefault(f => f.Name == targetFolderName);
                    if (targetFolder == null)
                    {
                        e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", new IMAP_t_orc_Unknown("TRYCREATE"), $"Folder '{targetFolderName}' does not exist");
                        return;
                    }

                    var imapState = dbContext.ImapState.SingleOrDefault();
                    if (imapState == null)
                    {
                        log.Error("No IMAP state found in database");
                        e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", "Internal server error during message processing");
                        return;
                    }

                    var sourceIds = e.MessagesInfo.Select(m => new Guid(m.ID)).ToList();
                    var sourceMessages = dbContext.Messages
                        .Include(m => m.Session)
                        .Where(m => sourceIds.Contains(m.Id))
                        .OrderBy(m => m.ImapUid)
                        .ToList();

                    foreach (var source in sourceMessages)
                    {
                        imapState.LastUid = Math.Max(0, imapState.LastUid + 1);
                        dbContext.Messages.Add(new DbModel.Message
                        {
                            Id = Guid.NewGuid(),
                            ImapUid = imapState.LastUid,
                            Mailbox = mailbox,
                            MailboxFolder = targetFolder,
                            MailboxFolderId = targetFolder.Id,
                            Session = source.Session,
                            From = source.From,
                            To = source.To,
                            DeliveredTo = source.DeliveredTo,
                            ReceivedDate = source.ReceivedDate,
                            Subject = source.Subject,
                            Data = source.Data,
                            MimeParseError = source.MimeParseError,
                            SessionEncoding = source.SessionEncoding,
                            AttachmentCount = source.AttachmentCount,
                            IsUnread = source.IsUnread,
                            RelayError = source.RelayError,
                            SecureConnection = source.SecureConnection,
                            EightBitTransport = source.EightBitTransport,
                            HasBareLineFeed = source.HasBareLineFeed,
                            MimeMetadata = source.MimeMetadata,
                            BodyText = source.BodyText
                        });
                    }

                    dbContext.SaveChanges();
                    NotifyMessagesChanged(scope, mailbox.Name);
                    log.Information("Copied {count} message(s) from folder {sourceFolder} to folder {targetFolder}", sourceMessages.Count, e.SourceFolder, targetFolderName);
                }
            }

            private void Session_Append(object sender, IMAP_e_Append e)
            {
                var folderName = ImapFolderNames.Normalize(e.Folder);
                if (FolderExists(folderName))
                {
                    // Provide a memory stream for the IMAP server to write message data to
                    var messageStream = new MemoryStream();
                    e.Stream = messageStream;

                    // Subscribe to the Completed event to process the message after data is received
                    e.Completed += (completedSender, completedArgs) =>
                    {
                        try
                        {
                            using (var scope = this.serviceScopeFactory.CreateScope())
                            {
                                var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                                if (messagesRepository == null)
                                {
                                    log.Error("IMAP APPEND failed - MessagesRepository service not available");
                                    return;
                                }

                                var dbContext = messagesRepository.DbContext;
                                if (dbContext == null)
                                {
                                    log.Error("IMAP APPEND failed - Database context not available");
                                    return;
                                }

                                // Read message data from stream
                                messageStream.Position = 0;
                                byte[] messageData = messageStream.ToArray();

                                if (messageData.Length == 0)
                                {
                                    log.Error("IMAP APPEND failed - No message data received in stream");
                                    return;
                                }

                                // Parse the message
                                var mail = Mail_Message.ParseFromByte(messageData);

                                // Convert to our Message entity
                                var message = new DbModel.Message
                                {
                                    Id = Guid.NewGuid(),
                                    Data = messageData,
                                    ReceivedDate = e.InternalDate != DateTime.MinValue ? e.InternalDate : DateTime.UtcNow,
                                    From = mail.From?.ToString() ?? "",
                                    To = mail.To?.ToString() ?? "",
                                    Subject = mail.Subject ?? "",
                                    IsUnread = !e.Flags.Contains("Seen", StringComparer.OrdinalIgnoreCase),
                                    AttachmentCount = mail.Attachments.Count()
                                };

                                // Find the mailbox and folder
                                var mailboxName = GetMailboxName();
                                var mailbox = dbContext.Mailboxes.Include(m => m.MailboxFolders)
                                    .FirstOrDefault(m => m.Name == mailboxName);

                                if (mailbox == null)
                                {
                                    log.Error("IMAP APPEND failed - Mailbox not found. MailboxName: {mailboxName}", mailboxName);
                                    return;
                                }

                                var folder = mailbox.MailboxFolders?.FirstOrDefault(f => f.Name == folderName);
                                if (folder == null)
                                {
                                    log.Error("Folder {folder} not found in mailbox {mailboxName}", folderName, mailboxName);
                                    return;
                                }

                                message.Mailbox = mailbox;
                                message.MailboxFolder = folder;
                                message.MailboxFolderId = folder.Id;

                                // Assign IMAP UID
                                var imapState = dbContext.ImapState.SingleOrDefault();
                                if (imapState == null)
                                {
                                    log.Error("No IMAP state found in database");
                                    return;
                                }

                                imapState.LastUid = Math.Max(0, imapState.LastUid + 1);
                                message.ImapUid = imapState.LastUid;

                                dbContext.SaveChanges();

                                messagesRepository.AddMessage(message).Wait();

                                e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "OK", $"APPEND completed");
                                log.Information("Successfully appended message to folder {folder} with UID {uid}", e.Folder, message.ImapUid);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex, "Error processing APPEND command for folder {folder}", e.Folder);
                            e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", "Internal server error during message processing");
                        }
                        finally
                        {
                            messageStream?.Dispose();
                        }
                    };
                }
                else
                {
                    e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", new IMAP_t_orc_Unknown("TRYCREATE"), $"Folder '{folderName}' does not exist");
                }
            }

            private void Session_Search(object sender, IMAP_e_Search e)
            {
                try
                {
                    var condition = new ImapSearchTranslator().Translate(e.Criteria);

                    using (var scope = this.serviceScopeFactory.CreateScope())
                    {
                        var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                        var folderName = ImapFolderNames.Normalize(this.session.SelectedFolderName ?? MailboxFolder.INBOX);
                        foreach (var unseenMessage in messagesRepository.GetMessages(GetMailboxName(), folderName, true).Where(condition))
                        {
                            e.AddMessage(unseenMessage.ImapUid);
                        }
                    }
                }
                catch (ImapSearchCriteriaNotSupportedException ex)
                {
                    e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", ex.Message);
                }




            }

            private void Session_Select(object sender, IMAP_e_Select e)
            {
                var folderName = ImapFolderNames.Normalize(e.Folder);
                MailboxFolder folder;
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                    folder = GetMailbox(messagesRepository.DbContext).MailboxFolders.FirstOrDefault(f => f.Name == folderName);
                }

                if (folder == null)
                {
                    e.ErrorResponse = new IMAP_r_ServerStatus(e.CmdTag, "NO", $"Folder '{folderName}' does not exist");
                    return;
                }

                e.Flags.Clear();
                e.Flags.Add("\\Deleted");
                e.Flags.Add("\\Seen");

                e.PermanentFlags.Clear();
                e.PermanentFlags.Add("\\Deleted");
                e.PermanentFlags.Add("\\Seen");

                // UIDVALIDITY. IMAP UIDs come from a global counter and are never reused, so a constant is sufficient.
                e.FolderUID = folderName == "INBOX" ? 1234 : 5678;
            }

            private void Session_Store(object sender, IMAP_e_Store e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

                    if (e.FlagsSetType == IMAP_Flags_SetType.Add || e.FlagsSetType == IMAP_Flags_SetType.Replace)
                    {
                        if (e.Flags.Contains("Seen", StringComparer.OrdinalIgnoreCase))
                        {
                            messagesRepository.MarkMessageRead(new Guid(e.MessageInfo.ID)).Wait();
                        }

                        if (e.Flags.Contains("Deleted", StringComparer.OrdinalIgnoreCase))
                        {
                            messagesRepository.DeleteMessage(new Guid(e.MessageInfo.ID)).Wait();
                        }
                    }
                }
            }

            private readonly ScriptingHost scriptingHost;
            private readonly IOptionsMonitor<ServerOptions> serverOptions;
            private readonly IServiceScopeFactory serviceScopeFactory;
            private readonly IMAP_Session session;

            private void Session_GetMessagesInfo(object sender, IMAP_e_MessagesInfo e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

                    foreach (var message in messagesRepository.GetMessages(GetMailboxName(), ImapFolderNames.Normalize(e.Folder), true).OrderBy(m => m.ImapUid))
                    {
                        List<string> flags = new List<string>();
                        if (!message.IsUnread)
                        {
                            flags.Add("Seen");
                        }

                        e.MessagesInfo.Add(new IMAP_MessageInfo(message.Id.ToString(), message.ImapUid, flags.ToArray(), message.Data.Length, message.ReceivedDate));
                    }
                }
            }

            private Mailbox GetMailbox(Smtp4devDbContext dbContext)
            {
                var mailboxName = GetMailboxName();
                return dbContext.Mailboxes.Include(m => m.MailboxFolders).Single(m => m.Name == mailboxName);
            }

            private bool FolderExists(string folderName)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                    var mailboxName = GetMailboxName();
                    return messagesRepository.DbContext.MailboxFolders.Any(f => f.Mailbox.Name == mailboxName && f.Name == folderName);
                }
            }

            private List<(string Name, string[] Attributes)> GetMatchingFolders(string referenceName, string pattern)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                    var mailboxName = GetMailboxName();
                    var folderNames = messagesRepository.DbContext.MailboxFolders
                        .Where(f => f.Mailbox.Name == mailboxName)
                        .Select(f => f.Name)
                        .ToList()
                        .OrderBy(SortDefaultFoldersFirst).ThenBy(f => f, StringComparer.Ordinal)
                        .ToList();

                    return folderNames
                        .Where(f => ImapFolderNames.IsMatch(f, referenceName, pattern))
                        .Select(f => (f, new[] { folderNames.Any(c => ImapFolderNames.IsDescendantOf(c, f)) ? "\\HasChildren" : "\\HasNoChildren" }))
                        .ToList();
                }
            }

            private static int SortDefaultFoldersFirst(string folderName)
            {
                if (folderName == MailboxFolder.INBOX)
                {
                    return 0;
                }

                if (folderName == MailboxFolder.SENT)
                {
                    return 1;
                }

                return 2;
            }

            private static void NotifyMessagesChanged(IServiceScope scope, string mailboxName)
            {
                scope.ServiceProvider.GetService<NotificationsHub>()?.OnMessagesChanged(mailboxName).Wait();
            }

            private string GetMailboxName()
            {
                var configUser = this.serverOptions.CurrentValue.Users?.FirstOrDefault(u => this.session.AuthenticatedUserIdentity.Name.Equals(u.Username, StringComparison.OrdinalIgnoreCase));

                return configUser?.DefaultMailbox ?? "Default";
            }

            private void Session_Fetch(object sender, IMAP_e_Fetch e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

                    foreach (var msgInfo in e.MessagesInfo)
                    {
                        var dbMessage = messagesRepository.GetAllMessages().SingleOrDefault(m => m.Id == new Guid(msgInfo.ID));

                        if (dbMessage != null)
                        {
                            ApiModel.Message apiMessage = new ApiModel.Message(dbMessage);
                            Mail_Message message = Mail_Message.ParseFromByte(apiMessage.Data);
                            e.AddData(msgInfo, message);
                        }
                    }
                }

            }

            private void Session_Login(object sender, IMAP_e_Login e)
            {
                if (!serverOptions.CurrentValue.AuthenticationRequired)
                {
                    e.IsAuthenticated = true;
                    return;
                }

                var user = serverOptions.CurrentValue.Users?.FirstOrDefault(u => e.UserName.Equals(u.Username, StringComparison.CurrentCultureIgnoreCase));

                if (user != null && e.Password.Equals(user.Password))
                {
                    log.Information("IMAP authentication successful. Username: {username}, RemoteEndpoint: {remoteEndpoint}",
                        e.UserName, session.RemoteEndPoint);
                    e.IsAuthenticated = true;
                }
                else
                {
                    log.Warning("IMAP authentication failed. Username: {username}, RemoteEndpoint: {remoteEndpoint}",
                        e.UserName, session.RemoteEndPoint);
                    e.IsAuthenticated = false;
                }
            }

            private void Session_List(object sender, IMAP_e_List e)
            {
                foreach (var folder in GetMatchingFolders(e.FolderReferenceName, e.FolderFilter))
                {
                    e.Folders.Add(new IMAP_r_u_List(folder.Name, ImapFolderNames.Delimiter, folder.Attributes));
                }
            }

            private void Session_LSub(object sender, IMAP_e_LSub e)
            {
                // Subscriptions are not tracked, so all folders are treated as subscribed
                foreach (var folder in GetMatchingFolders(e.FolderReferenceName, e.FolderFilter))
                {
                    e.Folders.Add(new IMAP_r_u_LSub(folder.Name, ImapFolderNames.Delimiter, folder.Attributes));
                }
            }
        }
    }
}
