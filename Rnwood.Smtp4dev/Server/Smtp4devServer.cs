using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    public class Smtp4devServer
    {
        public Smtp4devServer(Func<Smtp4devDbContext> dbContextFactory, IOptions<ServerOptions> serverOptions, MessagesHub messagesHub, SessionsHub sessionsHub)
        {
            this.dbContextFactory = dbContextFactory;

            this.smtpServer = new DefaultServer(serverOptions.Value.AllowRemoteConnections, serverOptions.Value.Port);
            this.smtpServer.MessageReceivedEventHandler += OnMessageReceived;
            this.smtpServer.SessionCompletedEventHandler += OnSessionCompleted;
            this.smtpServer.SessionStartedHandler += OnSessionStarted;

            this.messagesHub = messagesHub;
            this.sessionsHub = sessionsHub;

            this.serverOptions = serverOptions; ;
        }

        private IOptions<ServerOptions> serverOptions;
        private readonly IDictionary<ISession, Guid> sessionToDbId = new Dictionary<ISession, Guid>();

        private async Task OnSessionStarted(object sender, SessionEventArgs e)
        {
            Console.WriteLine($"Session started. Client address {e.Session.ClientAddress}");
            Smtp4devDbContext dbContent = dbContextFactory();

            Session session = new Session();
            await UpdateDbSession(e, session);
            dbContent.Sessions.Add(session);
            dbContent.SaveChanges();

            sessionToDbId[e.Session] = session.Id;
        }

        private static async Task UpdateDbSession(SessionEventArgs e, Session session)
        {
            session.StartDate = e.Session.StartDate;
            session.EndDate = e.Session.EndDate;
            session.ClientAddress = e.Session.ClientAddress.ToString();
            session.ClientName = e.Session.ClientName;
            session.NumberOfMessages = (await e.Session.GetMessages()).Count;
            session.Log = (await e.Session.GetLog()).ReadToEnd();
            session.SessionErrorType = e.Session.SessionErrorType;
            session.SessionError = e.Session.SessionError?.ToString();
        }

        private async Task OnSessionCompleted(object sender, SessionEventArgs e)
        {
            int messageCount = (await e.Session.GetMessages()).Count;

            Console.WriteLine($"Session completed. Client address {e.Session.ClientAddress}. Number of messages {messageCount}");
            Smtp4devDbContext dbContent = dbContextFactory();

            Session session = dbContent.Sessions.Find(sessionToDbId[e.Session]);
            await UpdateDbSession(e, session);
            dbContent.SaveChanges();

            TrimSessions(dbContent);
            dbContent.SaveChanges();


            sessionsHub.OnSessionsChanged().Wait();
        }

        private async Task OnMessageReceived(object sender, MessageEventArgs e)
        {
            Smtp4devDbContext dbContent = dbContextFactory();

            using (Stream stream = await e.Message.GetData())
            {
                Message message = await new MessageConverter().ConvertAsync(stream, e.Message.From, string.Join(", ", e.Message.Recipients));

                Session session = dbContent.Sessions.Find(sessionToDbId[e.Message.Session]);
                message.Session = session;
                dbContent.Messages.Add(message);
            }

            await dbContent.SaveChangesAsync();

            TrimMessages(dbContent);
            await dbContent.SaveChangesAsync();
            await messagesHub.OnMessagesChanged();
        }

        private void TrimMessages(Smtp4devDbContext dbContext)
        {
            dbContext.Messages.RemoveRange(dbContext.Messages.OrderByDescending(m => m.ReceivedDate).Skip(serverOptions.Value.NumberOfMessagesToKeep));
        }

        private void TrimSessions(Smtp4devDbContext dbContext)
        {
            dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.EndDate.HasValue).OrderByDescending(m => m.EndDate).Skip(serverOptions.Value.NumberOfSessionsToKeep));
        }

        private readonly Func<Smtp4devDbContext> dbContextFactory;

        private DefaultServer smtpServer;

        private MessagesHub messagesHub;
        private SessionsHub sessionsHub;

        public void Start()
        {
            Smtp4devDbContext dbContent = dbContextFactory();
            
            foreach(Session unfinishedSession in dbContent.Sessions.Where(s => !s.EndDate.HasValue))
            {
                unfinishedSession.EndDate = DateTime.Now;
            }
            dbContent.SaveChanges();

            TrimMessages(dbContent);
            dbContent.SaveChanges();

            TrimSessions(dbContent);
            dbContent.SaveChanges();
            messagesHub.OnMessagesChanged().Wait();
            sessionsHub.OnSessionsChanged().Wait();

            smtpServer.Start();
            Console.WriteLine($"SMTP Server is listening on port {smtpServer.PortNumber}. Keeping last {serverOptions.Value.NumberOfMessagesToKeep} messages and {serverOptions.Value.NumberOfSessionsToKeep} sessions.");

        }
    }
}
