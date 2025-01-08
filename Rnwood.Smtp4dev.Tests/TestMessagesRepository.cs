using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Tests
{
    internal class TestMessagesRepository : IMessagesRepository
    {
        public TestMessagesRepository(params Message[] messages)
        {
            Messages.AddRange(messages);
        }

        public List<DbModel.Message> Messages { get; } = new List<Message>();

        public Task DeleteAllMessages(string mailboxName)
        {
            Messages.Clear();
            return Task.CompletedTask;
        }

        public Smtp4devDbContext DbContext => throw new NotImplementedException();
        public Task CopyMessageToFolder(Guid id, string targetFolder)
        {
            var message = Messages.FirstOrDefault(m => m.Id == id);
            if (message != null)
            {
                message.Folder = new Folder() { Name = targetFolder, Path = targetFolder };
            }
            return Task.CompletedTask;
        }

        public IQueryable<Message> GetMessages(string mailboxName, string folder = null, bool unTracked = true)
        {
            return Messages.AsQueryable();
        }

        public Task DeleteMessage(Guid id)
        {
            Messages.RemoveAll(m => m.Id == id);
            return Task.CompletedTask;
        }

        public IQueryable<Message> GetAllMessages(bool unTracked = true)
        {
            return Messages.AsQueryable();
        }

        public Task MarkAllMessagesRead(string mailboxName)
        {
            foreach (var msg in Messages)
            {
                msg.IsUnread = false;
            }

            return Task.CompletedTask;
        }

        public Task MarkMessageRead(Guid id)
        {
            Messages.FirstOrDefault(m => m.Id == id).IsUnread = false;
            return Task.CompletedTask;
        }

        public Task<Message> TryGetMessageById(Guid id, bool tracked)
        {
            return Task.FromResult( Messages.SingleOrDefault(m => m.Id == id));
        }
    }
}