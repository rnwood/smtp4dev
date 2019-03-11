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

        private static async Task UpdateDbSession(ISession session, Session dbSession)
        {
            dbSession.StartDate = session.StartDate;
            dbSession.EndDate = session.EndDate;
            dbSession.ClientAddress = session.ClientAddress.ToString();
            dbSession.ClientName = session.ClientName;
            dbSession.NumberOfMessages = (await session.GetMessages()).Count;
            dbSession.Log = (await session.GetLog()).ReadToEnd();
            dbSession.SessionErrorType = session.SessionErrorType;
            dbSession.SessionError = session.SessionError?.ToString();
        }

        internal Task MarkMessageRead(Guid id)
        {
            return QueueTask(async() => {
                Smtp4devDbContext dbContent = dbContextFactory();
                DbModel.Message message = await dbContent.Messages.FindAsync(id);

                if (message.IsUnread)
                {
                    message.IsUnread = false;
                    dbContent.SaveChanges();
                    await messagesHub.OnMessagesChanged();
                }
            }, true);
        }

        private async Task OnSessionStarted(object sender, SessionEventArgs e)
        {
            Console.WriteLine($"Session started. Client address {e.Session.ClientAddress}.");
            await QueueTask(async () => {

                Smtp4devDbContext dbContent = dbContextFactory();

                Session dbSession = new Session();
                await UpdateDbSession(e.Session, dbSession);
                dbContent.Sessions.Add(dbSession);
                dbContent.SaveChanges();

                sessionToDbId[e.Session] = dbSession.Id;

            }, false);
        }

        private async Task OnSessionCompleted(object sender, SessionEventArgs e)
        {
            int messageCount = (await e.Session.GetMessages()).Count;
            Console.WriteLine($"Session completed. Client address {e.Session.ClientAddress}. Number of messages {messageCount}.");

            await QueueTask(async () =>
            {
                Smtp4devDbContext dbContent = dbContextFactory();

                Session dbSession = dbContent.Sessions.Find(sessionToDbId[e.Session]);
                await UpdateDbSession(e.Session, dbSession);
                await dbContent.SaveChangesAsync();

                Smtp4devServer.TrimSessions(dbContent, serverOptions.Value);
                await dbContent.SaveChangesAsync();

                await sessionsHub.OnSessionsChanged();

            }, false);
        }

        private Task QueueTask(Func<Task> task, bool priority)
        {
            Task result = new Task(async () =>
            {
                await task();
            });

            (priority ? priorityProcessingQueue : processingQueue).Add(() =>
            {
                result.Start();
                return result;
            });

            return result;
        }

        internal Task DeleteSession(Guid id)
        {
            return QueueTask(async () =>
            {
                Smtp4devDbContext dbContext = dbContextFactory();
                dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.Id == id));

                dbContext.SaveChanges();

                await sessionsHub.OnSessionsChanged();
            }, true);
        }

        internal Task DeleteAllSessions()
        {
            return QueueTask(async () =>
            {
                Smtp4devDbContext dbContext = dbContextFactory();
                dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.EndDate.HasValue));
                dbContext.SaveChanges();
                await sessionsHub.OnSessionsChanged();
            }, true);
        }

        private async Task OnMessageReceived(object sender, MessageEventArgs e)
        {
            using (Stream stream = await e.Message.GetData())
            {
                string to = string.Join(", ", e.Message.Recipients);
                Console.WriteLine($"Message received. Client address {e.Message.Session.ClientAddress}. From {e.Message.From}. To {to}.");
                Message message = await new MessageConverter().ConvertAsync(stream, e.Message.From, to);
                message.IsUnread = true;

                await QueueTask(async () => {
                    Console.WriteLine("Processing received message");
                    Smtp4devDbContext dbContext = dbContextFactory();

                    Session dbSession = dbContext.Sessions.Find(sessionToDbId[e.Message.Session]);
                    message.Session = dbSession;
                    dbContext.Messages.Add(message);

                    await dbContext.SaveChangesAsync();

                    Smtp4devServer.TrimMessages(dbContext, serverOptions.Value);
                    await dbContext.SaveChangesAsync();
                    await messagesHub.OnMessagesChanged();
                }, false);
            }
        }

        internal Task DeleteMessage(Guid id)
        {
            return QueueTask(async () =>
            {
                Smtp4devDbContext dbContext = dbContextFactory();
                dbContext.Messages.RemoveRange(dbContext.Messages.Where(m => m.Id == id));
                await dbContext.SaveChangesAsync();
                await messagesHub.OnMessagesChanged();
            }, true);
        }


        internal Task DeleteAllMessages()
        {
            return QueueTask(async () =>
            {
                Smtp4devDbContext dbContext = dbContextFactory();
                dbContext.Messages.RemoveRange(dbContext.Messages);
                await dbContext.SaveChangesAsync();
                await messagesHub.OnMessagesChanged();
            }, true);
        }



        private static void TrimMessages(Smtp4devDbContext dbContext, ServerOptions serverOptions)
        {
            dbContext.Messages.RemoveRange(dbContext.Messages.OrderByDescending(m => m.ReceivedDate).Skip(serverOptions.NumberOfMessagesToKeep));
        }

        private static void TrimSessions(Smtp4devDbContext dbContext, ServerOptions serverOptions)
        {
            dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.EndDate.HasValue).OrderByDescending(m => m.EndDate).Skip(serverOptions.NumberOfSessionsToKeep));
        }

        private async Task ProcessingTaskWork()
        {
            Console.WriteLine("Message/session consuming thread running.");
            while (!processingQueue.IsCompleted && !priorityProcessingQueue.IsCompleted)
            {
                Func<Task> nextItem = null;
                try
                {
                    BlockingCollection<Func<Task>>.TakeFromAny(new[] { priorityProcessingQueue, processingQueue }, out nextItem);
                }
                catch (InvalidOperationException)
                {
                    if (processingQueue.IsCompleted || priorityProcessingQueue.IsCompleted)
                    {
                        break;
                    }

                    throw;
                }

                await nextItem();
            }

            Console.WriteLine("Message/session consuming thread ending.");
        }

        private readonly Func<Smtp4devDbContext> dbContextFactory;

        private BlockingCollection<Func<Task>> processingQueue;

        private BlockingCollection<Func<Task>> priorityProcessingQueue;

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

            processingQueue = new BlockingCollection<Func<Task>>();
            priorityProcessingQueue = new BlockingCollection<Func<Task>>();
            Task.Run(ProcessingTaskWork);

            smtpServer.Start();

            Console.WriteLine($"SMTP Server is listening on port {smtpServer.PortNumber}. Keeping last {serverOptions.Value.NumberOfMessagesToKeep} messages and {serverOptions.Value.NumberOfSessionsToKeep} sessions.");

        }
    }
}
