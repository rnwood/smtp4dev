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
using System.Text.RegularExpressions;
using Rnwood.SmtpServer;
using System.IO;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MimeKit;
using static System.Formats.Asn1.AsnWriter;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Server
{
    public partial class ImapServer
    {
        class SessionHandler
        {
            private const char HIERARCHY_SEPARATOR = '/';
            private readonly ILogger log = Log.ForContext<SessionHandler>();
            public SessionHandler(IMAP_Session session, ScriptingHost scriptingHost, IOptionsMonitor<Settings.ServerOptions> serverOptions, IServiceScopeFactory serviceScopeFactory)
            {
                this.session = session;
                session.Create += Session_Create;
                session.Append += Session_Append;
                session.List += Session_List;
                session.LSub += Session_LSub;
                session.Login += Session_Login;
                session.Fetch += Session_Fetch;
                session.Subscribe += Session_Subscribe;
                session.GetMessagesInfo += Session_GetMessagesInfo;
                session.Capabilities.Remove("QUOTA");
                session.Capabilities.Remove("NAMESPACE");
                session.Capabilities.Remove("UTF8=ACCEPT");
                session.Store += Session_Store;
                session.Select += Session_Select;
                session.Search += Session_Search;
                session.Copy += Session_Copy;
                session.Namespace += Session_Namespace;
                session.Delete += Session_Delete;

                this.scriptingHost = scriptingHost;
                this.serverOptions = serverOptions;
                this.serviceScopeFactory = serviceScopeFactory;
            }

            private void Session_Delete(object sender, IMAP_e_Folder e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var mailboxRepository = scope.ServiceProvider.GetService<IMailboxRepository>();
                    var folderRepository = scope.ServiceProvider.GetService<IFolderRepository>();

                    folderRepository.DeleteFolder(e.Folder.TrimEnd(HIERARCHY_SEPARATOR), mailboxRepository.GetMailboxByName(GetMailboxName())).Wait();
                }
            }

            private void Session_Namespace(object sender, IMAP_e_Namespace e)
            {
                e.NamespaceResponse = new IMAP_r_u_Namespace([new IMAP_Namespace_Entry("", HIERARCHY_SEPARATOR)], [], []);
            }

            private void Session_Subscribe(object sender, IMAP_e_Folder e)
            {
            }

            private void Session_Create(object sender, IMAP_e_Folder e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var mailboxRepository = scope.ServiceProvider.GetService<IMailboxRepository>();
                    var folderRepository = scope.ServiceProvider.GetService<IFolderRepository>();

                    folderRepository.CreateFolder(e.Folder.TrimEnd(HIERARCHY_SEPARATOR), mailboxRepository.GetMailboxByName(GetMailboxName())).Wait();
                }
            }

            private void Session_Append(object sender, IMAP_e_Append e)
            {



                var stream = new MemoryStream(e.Size);
                e.Completed += (s, ea) =>
                {
                    using var scope = this.serviceScopeFactory.CreateScope();
                    stream.Position = 0;
                    MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();

                    var mimeMessage = MimeMessage.Load(stream);

                    messageBuilder.From = string.Join(",", mimeMessage.From.Select(f => f.ToString()));
                    foreach (var to in mimeMessage.To.Select(t => t.ToString()))
                    {
                        messageBuilder.Recipients.Add(to);
                    }
                    messageBuilder.ReceivedDate = DateTime.Now;
                    messageBuilder.DeclaredMessageSize = e.Size;
                    messageBuilder.SecureConnection = session.IsSecureConnection;
                    messageBuilder.EightBitTransport = true;
                    using (var targetStream = messageBuilder.WriteData().Result)
                    {
                        stream.Position = 0;
                        stream.CopyTo(targetStream);
                    }
                    var message = messageBuilder.ToMessage().Result;

                    var dbMessage = new MessageConverter().ConvertAsync(message, []).Result;


                    var mailboxRepository = scope.ServiceProvider.GetService<IMailboxRepository>();
                    dbMessage.Mailbox = mailboxRepository.GetMailboxByName(this.GetMailboxName());
                    var folderRepository = scope.ServiceProvider.GetService<IFolderRepository>();
                    dbMessage.Folder = folderRepository.GetFolderOrCreate(e.Folder, dbMessage.Mailbox);

                    scope.ServiceProvider.GetService<IMessagesRepository>().AddMessage(dbMessage).Wait();


                };
                e.Stream = stream;

            }

            private void Session_Search(object sender, IMAP_e_Search e)
            {
                try
                {
                    var condition = new ImapSearchTranslator().Translate(e.Criteria);

                    using (var scope = this.serviceScopeFactory.CreateScope())
                    {
                        var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                        foreach (var unseenMessage in messagesRepository.GetMessages(GetMailboxName(), null, true).Where(condition))
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

                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var mailboxRepository = scope.ServiceProvider.GetService<IMailboxRepository>();
                    var folderRepository = scope.ServiceProvider.GetService<IFolderRepository>();

                    var folder = folderRepository.GetFolderOrCreate(e.Folder, mailboxRepository.GetMailboxByName(GetMailboxName()));


                    e.FolderUID = e.Folder.GetHashCode();
                }
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

            private void Session_Copy(object sender, IMAP_e_Copy e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                    foreach (var message in e.MessagesInfo)
                    {
                        messagesRepository.CopyMessageToFolder(new Guid(message.ID), e.TargetFolder).Wait();
                    }

                }
            }

            private readonly ScriptingHost scriptingHost;
            private readonly IOptionsMonitor<Settings.ServerOptions> serverOptions;
            private readonly IServiceScopeFactory serviceScopeFactory;
            private readonly IMAP_Session session;

            private void Session_GetMessagesInfo(object sender, IMAP_e_MessagesInfo e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

                    foreach (var message in messagesRepository.GetMessages(GetMailboxName(), e.Folder))
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
                        var dbMessage = messagesRepository.TryGetMessageById(new Guid(msgInfo.ID), false).Result;

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
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var folderRepository = scope.ServiceProvider.GetService<IFolderRepository>();
                    var mailboxRepository = scope.ServiceProvider.GetService<IMailboxRepository>();

                    char delimiter = HIERARCHY_SEPARATOR;

                    string regex = Regex.Escape((!string.IsNullOrEmpty(e.FolderReferenceName) ? $"{e.FolderReferenceName}{delimiter}" : "") + e.FolderFilter)
                        .Replace("\\*", ".+")
                        .Replace("%", $"[^{Regex.Escape(delimiter.ToString())}]+");

                    var allFolders = folderRepository.GetAllFolders(mailboxRepository.GetMailboxByName(GetMailboxName()));
                    foreach (var folder in allFolders.Where(f => Regex.IsMatch(f.Path, regex)))
                    {
                        bool hasChildren = allFolders.Any(f => f.Path.StartsWith($"{folder.Path}{HIERARCHY_SEPARATOR}"));

                        e.Folders.Add(new IMAP_r_u_List(folder.Path, delimiter, [hasChildren ? "\\HasChildren" : "\\HasNoChildren"]));
                    }

                }

            }


            private void Session_LSub(object sender, IMAP_e_LSub e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var folderRepository = scope.ServiceProvider.GetService<IFolderRepository>();
                    var mailboxRepository = scope.ServiceProvider.GetService<IMailboxRepository>();
                    char delimiter = HIERARCHY_SEPARATOR;

                    string regex = Regex.Escape((!string.IsNullOrEmpty(e.FolderReferenceName) ? $"{e.FolderReferenceName}{delimiter}" : "") + e.FolderFilter)
                        .Replace("\\*", ".+")
                        .Replace("%", $"[^{Regex.Escape(delimiter.ToString())}]+");

                    var allFolders = folderRepository.GetAllFolders(mailboxRepository.GetMailboxByName(GetMailboxName()));
                    foreach (var folder in allFolders.Where(f => Regex.IsMatch(f.Path, regex)))
                    {
                        bool hasChildren = allFolders.Any(f => f.Path.StartsWith($"{folder.Path}{HIERARCHY_SEPARATOR}"));
                        e.Folders.Add(new IMAP_r_u_LSub(folder.Path, delimiter, [hasChildren ? "\\HasChildren" : "\\HasNoChildren"]));
                    }
                }
            }
        }
    }
}
