using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Server
{
    public class MessageReceivedDbUpdateEvent : IDbUpdateEvent
    {
        public MessageReceivedDbUpdateEvent(Message message, SmtpServer.ISession session, IDictionary<SmtpServer.ISession, Guid> sessionToDbId)
        {
            this.message = message;
            this.sessionToDbId = sessionToDbId;
            this.session = session;
        }

        private readonly Message message;

        private readonly IDictionary<SmtpServer.ISession, Guid> sessionToDbId;

        private readonly SmtpServer.ISession session;

        public async Task Process(Func<Smtp4devDbContext> dbContextFactory, Hubs.MessagesHub messagesHub, Hubs.SessionsHub sessionsHub, ServerOptions serverOptions)
        {
            Smtp4devDbContext dbContext = dbContextFactory();

            Session session = dbContext.Sessions.Find(sessionToDbId[this.session]);
            message.Session = session;
            dbContext.Messages.Add(message);

            await dbContext.SaveChangesAsync();

            Smtp4devServer.TrimMessages(dbContext, serverOptions);
            await dbContext.SaveChangesAsync();
            await messagesHub.OnMessagesChanged();
        }
    }
}
