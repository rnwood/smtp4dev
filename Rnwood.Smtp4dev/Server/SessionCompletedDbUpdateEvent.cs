using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;

namespace Rnwood.Smtp4dev.Server
{
    public class SessionCompletedDbUpdateEvent : IDbUpdateEvent
    {
        private readonly ISession session;
        private readonly IDictionary<SmtpServer.ISession, Guid> sessionToDbId;

        public SessionCompletedDbUpdateEvent(ISession session, IDictionary<SmtpServer.ISession, Guid> sessionToDbId)
        {
            this.session = session;
            this.sessionToDbId = sessionToDbId;
        }

        public async Task Process(Func<Smtp4devDbContext> dbContextFactory, MessagesHub messagesHub, SessionsHub sessionsHub, ServerOptions serverOptions)
        {

            Smtp4devDbContext dbContent = dbContextFactory();

            Session dbSession = dbContent.Sessions.Find(sessionToDbId[session]);
            await UpdateDbSession(session, dbSession);
            await dbContent.SaveChangesAsync();

            Smtp4devServer.TrimSessions(dbContent, serverOptions);
            await dbContent.SaveChangesAsync();

            await sessionsHub.OnSessionsChanged();

        }

        internal static async Task UpdateDbSession(ISession session, Session dbSession)
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

    }
}
