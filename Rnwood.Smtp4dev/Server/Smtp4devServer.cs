using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;
using System;
using System.IO;

namespace Rnwood.Smtp4dev.Server
{
    public class Smtp4devServer
    {
        public Smtp4devServer(Func<Smtp4devDbContext> dbContextFactory, IOptions<ServerOptions> serverOptions, MessagesHub messagesHub, SessionsHub sessionsHub)
        {
            this.dbContextFactory = dbContextFactory;

            this.smtpServer = new DefaultServer(serverOptions.Value.AllowRemoteConnections, serverOptions.Value.Port);
            this.smtpServer.MessageReceived += OnMessageReceived;
            this.smtpServer.SessionCompleted += OnSessionCompleted;

            this.messagesHub = messagesHub;
            this.sessionsHub = sessionsHub;
        }

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            Smtp4devDbContext dbContent = dbContextFactory();

            Session session = new Session();
            session.EndDate = e.Session.EndDate.GetValueOrDefault(DateTime.Now);
            session.ClientAddress = e.Session.ClientAddress.ToString();
            session.ClientName = e.Session.ClientName;
            session.NumberOfMessages = e.Session.GetMessages().Length;
            session.Log = e.Session.GetLog().ReadToEnd();
            dbContent.Sessions.Add(session);

            dbContent.SaveChanges();

            sessionsHub.OnSessionsChanged().Wait();
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Smtp4devDbContext dbContent = dbContextFactory();

            using (Stream stream = e.Message.GetData())
            {
                var convert = new MessageConverter().Convert(stream);
                dbContent.Messages.Add(convert.Item1);
                dbContent.MessageDatas.Add(convert.Item2);
            }

            dbContent.SaveChanges();
            messagesHub.OnMessagesChanged().Wait();
        }

        private Func<Smtp4devDbContext> dbContextFactory;

        private DefaultServer smtpServer;

        private MessagesHub messagesHub;
        private SessionsHub sessionsHub;

        public void Start()
        {
            smtpServer.Start();
        }
    }
}
