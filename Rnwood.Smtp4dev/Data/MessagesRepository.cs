using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.Data
{
    public class MessagesRepository : IMessagesRepository
    {
        private readonly ITaskQueue taskQueue;
        private readonly NotificationsHub notificationsHub;
        private readonly Smtp4devDbContext dbContext;
        private readonly ServerOptions serverOptions;

        public MessagesRepository(ITaskQueue taskQueue, NotificationsHub notificationsHub, Smtp4devDbContext dbContext, IOptions<ServerOptions> serverOptions)
        {
            this.taskQueue = taskQueue;
            this.notificationsHub = notificationsHub;
            this.dbContext = dbContext;
            this.serverOptions = serverOptions.Value;
        }

        public Task AddMessage(DbModel.Message message)
        {
            return taskQueue.QueueTask(() =>
            {
                ImapState imapState = dbContext.ImapState.Single();
                imapState.LastUid = Math.Max(0, imapState.LastUid + 1);
                message.ImapUid = imapState.LastUid;

                dbContext.Messages.Add(message);

                dbContext.SaveChanges();

                TrimMessages(dbContext);

                dbContext.SaveChanges();

                notificationsHub.OnMessagesChanged(message.Mailbox.Name).Wait();
            }, true);

        }

        private void TrimMessages(Smtp4devDbContext dbContext)
        {
            foreach (var mailbox in dbContext.Mailboxes)
            {
                dbContext.Messages.RemoveRange(dbContext.Messages.Where(m => m.Mailbox == mailbox).OrderByDescending(m => m.ReceivedDate)
                    .Skip(serverOptions.NumberOfMessagesToKeep));
            }
        }

        public Task TrimMessages()
        {
            return taskQueue.QueueTask(() =>
            {
                TrimMessages(dbContext);
            }, true);
        }

        public Task CopyMessageToFolder(Guid id, string targetFolder)
        {
            return taskQueue.QueueTask(() =>
            {
                var message = dbContext.Messages.Include(m => m.Mailbox).FirstOrDefault(m => m.Id == id);
                var folder = dbContext.Folders.FirstOrDefault(m => m.Name == targetFolder);
                message.Folder = folder;
                message.Id = Guid.NewGuid();
                dbContext.Messages.Add(message);
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged(message.Folder.Name).Wait();
            }, true);
        }

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

        public IQueryable<Message> GetMessages(string mailboxName, string folder = null, bool unTracked = true)
        {
            IQueryable<Message> query;
            if (folder == null)
            {
                query = dbContext.Messages.Where(m => m.Mailbox.Name == mailboxName);

            }
            else
            {
                query = dbContext.Messages.Where(m => m.Mailbox.Name == mailboxName && m.Folder.Name == folder);
            }
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
                dbContext.Messages.RemoveRange(dbContext.Messages.Where(m => m.Mailbox.Name == mailbox));
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged(mailbox).Wait();
            }, true);
        }

        public Task<Message> TryGetMessageById(Guid id, bool tracked)
        {
            return this.GetAllMessages(!tracked).SingleOrDefaultAsync(m => m.Id == id);
        }

        public Task UpdateMessage(Message message)
        {
            return taskQueue.QueueTask(() =>
            {
                dbContext.Update(message);
                dbContext.SaveChanges();
            }, true);
        }
    }
}