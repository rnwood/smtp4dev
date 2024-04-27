using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Data
{
    public class MessagesRepository : IMessagesRepository
    {
        private readonly ITaskQueue taskQueue;
        private readonly NotificationsHub notificationsHub;
        private readonly Smtp4devDbContext dbContext;

        public MessagesRepository(ITaskQueue taskQueue, NotificationsHub notificationsHub, Smtp4devDbContext dbContext)
        {
            this.taskQueue = taskQueue;
            this.notificationsHub = notificationsHub;
            this.dbContext = dbContext;
        }

        public Smtp4devDbContext DbContext => this.dbContext;

        public Task MarkAllMessagesRead(string mailbox)
        {
            return taskQueue.QueueTask(() =>
            {
                // More performant to bulk update but will need to test platform compat of SQLitePCLRaw.bundle_e_sqlite3 https://github.com/borisdj/EFCore.BulkExtensions
                var unReadMessages = dbContext.Messages.Where(m => m.Mailbox.Name == mailbox && m.IsUnread);
                foreach (var msg in unReadMessages)
                {
                    msg.IsUnread = false;
                }

                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged(mailbox).Wait();
            }, true);
        }

        public Task MarkMessageRead(Guid id)
        {
            return taskQueue.QueueTask(() =>
            {
                var message = dbContext.Messages.Include(m => m.Mailbox).FirstOrDefault(m => m.Id == id);
                if (message?.IsUnread != true) return;
                message.IsUnread = false;
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged(message.Mailbox.Name).Wait();
            }, true);
        }

        public IQueryable<Message> GetAllMessages(bool unTracked = true)
        {
            var query = dbContext.Messages;
            return unTracked ? query.AsNoTracking() : query;
        }

        public IQueryable<Message> GetMessages(string mailboxName, bool unTracked = true)
        {
            var query = dbContext.Messages.Where(m => m.Mailbox.Name == mailboxName);
            return unTracked ? query.AsNoTracking() : query;
        }

        public Task DeleteMessage(Guid id)
        {
            return taskQueue.QueueTask(() =>
            {
                var message = dbContext.Messages.Include(m => m.Mailbox).FirstOrDefault(m => m.Id == id);

                if (message != null)
                {
                    dbContext.Messages.Remove(message);
                    dbContext.SaveChanges();
                    notificationsHub.OnMessagesChanged(message.Mailbox.Name).Wait();
                }
            }, true);
        }

        public Task DeleteAllMessages(string mailbox)
        {
            return taskQueue.QueueTask(() =>
            {
                dbContext.Messages.RemoveRange(dbContext.Messages.Where(m=> m.Mailbox.Name == mailbox));
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged(mailbox).Wait();
            }, true);
        }

        public Task<Message> TryGetMessageById(Guid id, bool tracked)
        {
            return this.GetAllMessages(!tracked).SingleOrDefaultAsync(m => m.Id == id);
        }
        
    }
}