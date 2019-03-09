using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;

namespace Rnwood.Smtp4dev.Server
{
    public class SessionStartedDbUpdateEvent : IDbUpdateEvent
    {
        private readonly ISession session;
        private readonly IDictionary<SmtpServer.ISession, Guid> sessionToDbId;

        public SessionStartedDbUpdateEvent(ISession session, IDictionary<SmtpServer.ISession, Guid> sessionToDbId)
        {
            this.session = session;
            this.sessionToDbId = sessionToDbId;
        }

        public async Task Process(Func<Smtp4devDbContext> dbContextFactory, MessagesHub messagesHub, SessionsHub sessionsHub, ServerOptions value)
        {
            Smtp4devDbContext dbContent = dbContextFactory();

            Session dbSession = new Session();
            await SessionCompletedDbUpdateEvent.UpdateDbSession(session, dbSession);
            dbContent.Sessions.Add(dbSession);
            dbContent.SaveChanges();

            sessionToDbId[session] = dbSession.Id;
        }
    }
}
