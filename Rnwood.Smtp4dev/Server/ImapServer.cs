using LumiSoft.Net;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Server;
using LumiSoft.Net.Mail;
using LumiSoft.Net.Mime;
using LumiSoft.Net.MIME;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    public class ImapServer
    {
        public ImapServer(IMessagesRepository messagesRepository, IOptionsMonitor<ServerOptions> serverOptions, Func<Smtp4devDbContext> dbContextFactory)
        {
            this.messagesRepository = messagesRepository;
            this.serverOptions = serverOptions;

            IDisposable eventHandler = null;
            var obs = Observable.FromEvent<ServerOptions>(e => eventHandler = serverOptions.OnChange(e), e => eventHandler.Dispose());
            obs.Throttle(TimeSpan.FromMilliseconds(100)).Subscribe(OnServerOptionsChanged);

            var dbContext = dbContextFactory();
            if (dbContext.ImapState.FirstOrDefault() == null)
            {
                dbContext.Add(new ImapState
                {
                    Id = Guid.Empty,
                    LastUid = 1
                });
                dbContext.SaveChanges();
            }
        }

        private void OnServerOptionsChanged(ServerOptions serverOptions)
        {
            Stop();

            TryStart();
        }

        public bool IsRunning
        {
            get
            {
                return imapServer?.IsRunning ?? false;
            }
        }

        public void TryStart()
        {
            if (!serverOptions.CurrentValue.ImapPort.HasValue)
            {
                Console.WriteLine($"IMAP server disabled");
                return;
            }

            imapServer = new IMAP_Server()
            {
                Bindings = new[] { new IPBindInfo(Dns.GetHostName(), BindInfoProtocol.TCP, serverOptions.CurrentValue.AllowRemoteConnections ? IPAddress.Any : IPAddress.Loopback, serverOptions.CurrentValue.ImapPort.Value) },
                GreetingText = "smtp4dev"
            };
            imapServer.SessionCreated += (o, ea) => new SessionHandler(ea.Session, this.messagesRepository);

            imapServer.Start();
            Console.WriteLine($"IMAP Server listening on port {imapServer.Bindings[0].Port}");
        }

        public void Stop()
        {
            imapServer?.Stop();
            imapServer = null;
        }

        private IMessagesRepository messagesRepository;
        private IMAP_Server imapServer;
        private IOptionsMonitor<ServerOptions> serverOptions;

        private void Logger_WriteLog(object sender, LumiSoft.Net.Log.WriteLogEventArgs e)
        {
            Console.WriteLine(e.LogEntry.Text);
        }

        class SessionHandler
        {
            public SessionHandler(IMAP_Session session, IMessagesRepository messagesRepository)
            {
                this.session = session;
                session.List += Session_List;
                session.Login += Session_Login;
                session.Fetch += Session_Fetch;
                session.GetMessagesInfo += Session_GetMessagesInfo;
                session.Capabilities.Remove("QUOTA");
                session.Capabilities.Remove("NAMESPACE");
                session.Store += Session_Store;
                session.Select += Session_Select;
                this.messagesRepository = messagesRepository;
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
                if (e.FlagsSetType == IMAP_Flags_SetType.Add || e.FlagsSetType == IMAP_Flags_SetType.Replace)
                {
                    if (e.Flags.Contains("Seen", StringComparer.OrdinalIgnoreCase))
                    {
                        messagesRepository.MarkMessageRead(new Guid(e.MessageInfo.ID));
                    }

                    if (e.Flags.Contains("Deleted", StringComparer.OrdinalIgnoreCase))
                    {
                        messagesRepository.DeleteMessage(new Guid(e.MessageInfo.ID));
                    }
                }
            }


            private IMessagesRepository messagesRepository;
            private IMAP_Session session;

            private void Session_GetMessagesInfo(object sender, IMAP_e_MessagesInfo e)
            {
                if (e.Folder == "INBOX")
                {
                    foreach (var message in messagesRepository.GetMessages())
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

            private void Session_Fetch(object sender, IMAP_e_Fetch e)
            {
                foreach (var msgInfo in e.MessagesInfo)
                {
                    var dbMessage = this.messagesRepository.GetMessages().FirstOrDefault(m => m.Id == new Guid(msgInfo.ID));

                    if (dbMessage != null)
                    {
                        ApiModel.Message apiMessage = new ApiModel.Message(dbMessage);
                        Mail_Message message = Mail_Message.ParseFromByte(apiMessage.Data);
                        e.AddData(msgInfo, message);
                    }
                }

            }

            private void Session_Login(object sender, IMAP_e_Login e)
            {
                e.IsAuthenticated = true;
            }

            private void Session_List(object sender, IMAP_e_List e)
            {
                e.Folders.Add(new IMAP_r_u_List("INBOX", '/', new string[0]));

            }
        }
    }
}
