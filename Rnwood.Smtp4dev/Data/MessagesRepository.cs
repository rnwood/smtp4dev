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

        public Task MarkMessageRead(Guid id)
        {
            return taskQueue.QueueTask(() =>
            {
                var message = dbContext.Messages.FindAsync(id).Result;
                if (message?.IsUnread != true) return;
                message.IsUnread = false;
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged().Wait();
            }, true);
        }

        public IQueryable<Message> GetMessages(bool unTracked = true)
        {
            return unTracked ? dbContext.Messages.AsNoTracking() : dbContext.Messages;
        }

        public Task DeleteMessage(Guid id)
        {
            return taskQueue.QueueTask(() =>
            {
                dbContext.Messages.RemoveRange(dbContext.Messages.Where(m => m.Id == id));
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged().Wait();
            }, true);
        }

        public Task DeleteAllMessages()
        {
            return taskQueue.QueueTask(() =>
            {
                dbContext.Messages.RemoveRange(dbContext.Messages);
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged().Wait();
            }, true);
        }
    }
}