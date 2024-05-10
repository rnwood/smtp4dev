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
                e.Response = new IMAP_r_ServerStatus(e.Response.CommandTag, "NO", "APPEND is not supported");
            }

            private void Session_Search(object sender, IMAP_e_Search e)
            {
                try
                {
                    var condition = new ImapSearchTranslator().Translate(e.Criteria);

                    using (var scope = this.serviceScopeFactory.CreateScope())
                    {
                        var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();
                        foreach (var unseenMessage in messagesRepository.GetMessages(GetMailboxName(), true).Where(condition))
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

                    if (e.Folder == "INBOX")
                    {
                        foreach (var message in messagesRepository.GetMessages(GetMailboxName()))
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

            private void Session_Fetch(object sender, IMAP_e_Fetch e)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

                    foreach (var msgInfo in e.MessagesInfo)
                    {
                        var dbMessage = messagesRepository.GetMessages(GetMailboxName()).SingleOrDefault(m => m.Id == new Guid(msgInfo.ID));

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

                if (e.FolderFilter == "INBOX" || e.FolderFilter == "*")
                {
                    e.Folders.Add(new IMAP_r_u_List("INBOX", '/', ["\\HasNoChildren"]));
                }
            }

            private void Session_LSub(object sender, IMAP_e_LSub e)
            {
                if (e.FolderFilter == "INBOX" || e.FolderFilter == "*")
                {
                    e.Folders.Add(new IMAP_r_u_LSub("INBOX", '/', ["\\HasNoChildren"]));
                }
            }
        }
    }
}
