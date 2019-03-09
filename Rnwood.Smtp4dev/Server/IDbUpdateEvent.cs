using Rnwood.Smtp4dev.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    interface IDbUpdateEvent
    {
        Task Process(Func<Smtp4devDbContext> dbContextFactory, Hubs.MessagesHub messagesHub, Hubs.SessionsHub sessionsHub, ServerOptions value);
    }
}
