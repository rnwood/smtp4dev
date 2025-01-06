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

        public Task CreateMailbox(string mailbox)
        {
            return taskQueue.QueueTask(() =>
            {
                dbContext.Mailboxes.Add(new Mailbox()
                {
                    Name = mailbox,
                    Id = Guid.NewGuid(),
                });
                dbContext.SaveChanges();
            }, true);
        }

        public IQueryable<Mailbox> GetAllMailboxes()
        {
            var query = dbContext.Mailboxes;
            return query;
        }

        public Smtp4devDbContext DbContext => this.dbContext;

       
        
    }
}