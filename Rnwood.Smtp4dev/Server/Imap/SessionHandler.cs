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
using System.IO;
using MimeKit;

namespace Rnwood.Smtp4dev.Server
{
    public partial class ImapServer
    {
        class SessionHandler
        {
            // IMAP folder constants
            private const string INBOX_FOLDER = "INBOX";
            private const string SENT_FOLDER = "Sent";
            private readonly ILogger log = Log.ForContext<SessionHandler>();
            public SessionHandler(IMAP_Session session, ScriptingHost scriptingHost, IOptionsMonitor<ServerOptions> serverOptions, IServiceScopeFactory serviceScopeFactory)
            {
                this.session = session;
                session.Create += Session_Create;
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
                e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", "Folders are not supported");
            }

            private void Session_Append(object sender, IMAP_e_Append e)
            {
                // Only allow APPEND to the Sent folder for now
                if (e.Folder != SENT_FOLDER)
                {
                    e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", "APPEND is only supported for the Sent folder");
                    return;
                }

                try
                {
                    // Get the message stream and parse it
                    if (e.Stream == null)
                    {
                        e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", "No message data provided");
                        return;
                    }

                    using (var scope = this.serviceScopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
                        
                        // Read the message data
                        byte[] messageData = new byte[e.Size];
                        e.Stream.Read(messageData, 0, e.Size);
                        
                        // Parse the MIME message to extract headers
                        string subject = "";
                        string from = "";
                        string to = "";
                        string mimeParseError = null;
                        
                        try
                        {
                            using (var messageStream = new MemoryStream(messageData))
                            {
                                var mimeMessage = MimeMessage.Load(messageStream);
                                subject = mimeMessage.Subject ?? "";
                                from = mimeMessage.From.FirstOrDefault()?.ToString() ?? "";
                                to = string.Join(", ", mimeMessage.To.Select(addr => addr.ToString()));
                            }
                        }
                        catch (Exception ex)
                        {
                            mimeParseError = ex.Message;
                            log.Warning("Failed to parse MIME message for APPEND: {error}", ex.Message);
                        }

                        // Create a new Message entity
                        var message = new DbModel.Message
                        {
                            Id = Guid.NewGuid(),
                            From = from,
                            To = to,
                            Subject = subject,
                            Data = messageData,
                            ReceivedDate = e.InternalDate != DateTime.MinValue ? e.InternalDate : DateTime.Now,
                            MimeParseError = mimeParseError,
                            AttachmentCount = 0,
                            IsUnread = !e.Flags.Contains("\\Seen", StringComparer.OrdinalIgnoreCase),
                            SecureConnection = false,
                            SessionEncoding = System.Text.Encoding.UTF8.WebName
                        };

                        // Get or create the Sent mailbox
                        var sentMailboxName = GetFolderMailboxName(SENT_FOLDER);
                        var mailbox = dbContext.Mailboxes.FirstOrDefault(m => m.Name == sentMailboxName);
                        if (mailbox == null)
                        {
                            mailbox = new DbModel.Mailbox { Id = Guid.NewGuid(), Name = sentMailboxName };
                            dbContext.Mailboxes.Add(mailbox);
                        }
                        message.Mailbox = mailbox;

                        // Assign IMAP UID
                        var imapState = dbContext.ImapState.Single();
                        imapState.LastUid = Math.Max(0, imapState.LastUid + 1);
                        message.ImapUid = imapState.LastUid;

                        // Save the message
                        dbContext.Messages.Add(message);
                        dbContext.SaveChanges();

                        log.Information("APPEND successful: saved message to {folder} folder with UID {uid}", e.Folder, message.ImapUid);
                        e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "OK", "APPEND completed");
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, "APPEND operation failed");
                    e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", "APPEND failed: " + ex.Message);
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
                        var folderName = GetFolderMailboxName(session.SelectedFolderName);
                        foreach (var unseenMessage in messagesRepository.GetMessages(folderName, true).Where(condition))
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
                e.Flags.Clear();
                e.Flags.Add("\\Deleted");
                e.Flags.Add("\\Seen");

                e.PermanentFlags.Clear();
                e.PermanentFlags.Add("\\Deleted");
                e.PermanentFlags.Add("\\Seen");

                e.FolderUID = 1234;
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

                    if (e.Folder == INBOX_FOLDER || e.Folder == SENT_FOLDER)
                    {
                        var folderName = GetFolderMailboxName(e.Folder);
                        foreach (var message in messagesRepository.GetMessages(folderName))
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
            }

            private string GetMailboxName()
            {
                var configUser = this.serverOptions.CurrentValue.Users?.FirstOrDefault(u => this.session.AuthenticatedUserIdentity.Name.Equals(u.Username, StringComparison.OrdinalIgnoreCase));

                return configUser?.DefaultMailbox ?? "Default";
            }

            private string GetFolderMailboxName(string folderName)
            {
                if (folderName == INBOX_FOLDER)
                {
                    return GetMailboxName();
                }
                else if (folderName == SENT_FOLDER)
                {
                    return GetMailboxName() + "_Sent";
                }
                else
                {
                    return GetMailboxName();
                }
            }

            private void Session_Fetch(object sender, IMAP_e_Fetch e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

                    foreach (var msgInfo in e.MessagesInfo)
                    {
                        var folderName = GetFolderMailboxName(session.SelectedFolderName);
                        var dbMessage = messagesRepository.GetMessages(folderName).SingleOrDefault(m => m.Id == new Guid(msgInfo.ID));

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
                    log.Information("IMAP login success for user {user} to mailbox", e.UserName);
                    e.IsAuthenticated = true;
                }
                else
                {
                    log.Error("IMAP login failure for user {user}", e.UserName);
                    e.IsAuthenticated = false;
                }
            }

            private void Session_List(object sender, IMAP_e_List e)
            {

                if (e.FolderFilter == INBOX_FOLDER || e.FolderFilter == "*")
                {
                    e.Folders.Add(new IMAP_r_u_List(INBOX_FOLDER, '/', ["\\HasNoChildren"]));
                }
                
                if (e.FolderFilter == SENT_FOLDER || e.FolderFilter == "*")
                {
                    e.Folders.Add(new IMAP_r_u_List(SENT_FOLDER, '/', ["\\HasNoChildren"]));
                }
            }

            private void Session_LSub(object sender, IMAP_e_LSub e)
            {
                if (e.FolderFilter == INBOX_FOLDER || e.FolderFilter == "*")
                {
                    e.Folders.Add(new IMAP_r_u_LSub(INBOX_FOLDER, '/', ["\\HasNoChildren"]));
                }
                
                if (e.FolderFilter == SENT_FOLDER || e.FolderFilter == "*")
                {
                    e.Folders.Add(new IMAP_r_u_LSub(SENT_FOLDER, '/', ["\\HasNoChildren"]));
                }
            }
        }
    }
}
