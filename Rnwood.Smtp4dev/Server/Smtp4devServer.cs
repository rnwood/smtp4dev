using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;
using System;
using System.Collections.Concurrent;
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

        private Task OnSessionStarted(object sender, SessionEventArgs e)
        {
            Console.WriteLine($"Session started. Client address {e.Session.ClientAddress}.");
            processingQueue.Add(new SessionStartedDbUpdateEvent(e.Session, sessionToDbId));
            return Task.CompletedTask;
        }

        private async Task OnSessionCompleted(object sender, SessionEventArgs e)
        {
            int messageCount = (await e.Session.GetMessages()).Count;
            Console.WriteLine($"Session completed. Client address {e.Session.ClientAddress}. Number of messages {messageCount}.");
            processingQueue.Add(new SessionCompletedDbUpdateEvent(e.Session, sessionToDbId));
        }


        private async Task OnMessageReceived(object sender, MessageEventArgs e)
        {
            using (Stream stream = await e.Message.GetData())
            {
                string to = string.Join(", ", e.Message.Recipients);
                Console.WriteLine($"Message received. Client address {e.Message.Session.ClientAddress}. From {e.Message.From}. To {to}.");
                Message message = await new MessageConverter().ConvertAsync(stream, e.Message.From, to);
                processingQueue.Add(new MessageReceivedDbUpdateEvent(message, e.Message.Session, sessionToDbId));
            }

        }

        internal static void TrimMessages(Smtp4devDbContext dbContext, ServerOptions serverOptions)
        {
            dbContext.Messages.RemoveRange(dbContext.Messages.OrderByDescending(m => m.ReceivedDate).Skip(serverOptions.NumberOfMessagesToKeep));
        }

        internal static void TrimSessions(Smtp4devDbContext dbContext, ServerOptions serverOptions)
        {
            dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.EndDate.HasValue).OrderByDescending(m => m.EndDate).Skip(serverOptions.NumberOfSessionsToKeep));
        }

        private async Task ProcessingTaskWork()
        {
            Console.WriteLine("Message/session consuming thread running.");
            while (!processingQueue.IsCompleted)
            {
                IDbUpdateEvent nextItem = null;
                try
                {
                    nextItem = processingQueue.Take();
                }
                catch (InvalidOperationException)
                {
                    if (processingQueue.IsCompleted)
                    {
                        break;
                    }

                    throw;
                }

                await nextItem.Process(dbContextFactory, messagesHub, sessionsHub, serverOptions.Value);
            }

            Console.WriteLine("Message/session consuming thread ending.");
        }

        private readonly Func<Smtp4devDbContext> dbContextFactory;

        private BlockingCollection<IDbUpdateEvent> processingQueue;

        private DefaultServer smtpServer;

        private MessagesHub messagesHub;
        private SessionsHub sessionsHub;

        public void Start()
        {
            Smtp4devDbContext dbContent = dbContextFactory();

            foreach (Session unfinishedSession in dbContent.Sessions.Where(s => !s.EndDate.HasValue))
            {
                unfinishedSession.EndDate = DateTime.Now;
            }
            dbContent.SaveChanges();

            TrimMessages(dbContent, serverOptions.Value);
            dbContent.SaveChanges();

            TrimSessions(dbContent, serverOptions.Value);
            dbContent.SaveChanges();
            messagesHub.OnMessagesChanged().Wait();
            sessionsHub.OnSessionsChanged().Wait();

            processingQueue = new BlockingCollection<IDbUpdateEvent>();
            Task.Run(ProcessingTaskWork);

            smtpServer.Start();

            Console.WriteLine($"SMTP Server is listening on port {smtpServer.PortNumber}. Keeping last {serverOptions.Value.NumberOfMessagesToKeep} messages and {serverOptions.Value.NumberOfSessionsToKeep} sessions.");

        }
    }
}
