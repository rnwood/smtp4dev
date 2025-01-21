using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Data
{
    public class MailboxRepository : IMailboxRepository
    {
        private readonly ITaskQueue taskQueue;
        private readonly NotificationsHub notificationsHub;
        private readonly Smtp4devDbContext dbContext;

        public MailboxRepository(ITaskQueue taskQueue, NotificationsHub notificationsHub, Smtp4devDbContext dbContext)
        {
            this.taskQueue = taskQueue;
            this.notificationsHub = notificationsHub;
            this.dbContext = dbContext;
        }

        public IQueryable<Mailbox> GetAllMailboxes()
        {
            var query = dbContext.Mailboxes;
            return query;
        }

        public Mailbox GetMailboxByName(string name)
        {
            return dbContext.Mailboxes.FirstOrDefault(m => m.Name == name);
        }

        public Smtp4devDbContext DbContext => this.dbContext;

       
        
    }
}