using System;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;

namespace Rnwood.Smtp4dev.Server
{
    public class Smtp4devServer
    {
        private readonly Func<Smtp4devDbContext> dbContextFactory;

        private readonly MessagesHub messagesHub;
        private readonly SessionsHub sessionsHub;

        private readonly DefaultServer smtpServer;

        public Smtp4devServer(Func<Smtp4devDbContext> dbContextFactory, IOptions<ServerOptions> serverOptions,
            MessagesHub messagesHub, SessionsHub sessionsHub)
        {
            this.dbContextFactory = dbContextFactory;

            smtpServer = new DefaultServer(serverOptions.Value.AllowRemoteConnections, serverOptions.Value.Port);
            smtpServer.MessageReceived += OnMessageReceived;
            smtpServer.SessionCompleted += OnSessionCompleted;

            this.messagesHub = messagesHub;
            this.sessionsHub = sessionsHub;
        }

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            var dbContent = dbContextFactory();

            var session = new Session();
            session.EndDate = e.Session.EndDate.GetValueOrDefault(DateTime.Now);
            session.ClientAddress = e.Session.ClientAddress.ToString();
            session.ClientName = e.Session.ClientName;
            session.NumberOfMessages = e.Session.GetMessages().Length;
            session.Log = e.Session.GetLog().ReadToEnd();
            dbContent.Sessions.Add(session);

            dbContent.SaveChanges();

            sessionsHub.OnSessionsChanged().Wait();
        }

        private async void OnMessageReceived(object sender, MessageEventArgs e)
        {
            var dbContent = dbContextFactory();

            using (var stream = e.Message.GetData())
            {
                var message =
                    await new MessageConverter().ConvertAsync(stream, e.Message.From, string.Join(", ", e.Message.To));
                dbContent.Messages.Add(message);
            }

            dbContent.SaveChanges();
            messagesHub.OnMessagesChanged().Wait();
        }

        public void Start()
        {
            smtpServer.Start();
        }
    }
}