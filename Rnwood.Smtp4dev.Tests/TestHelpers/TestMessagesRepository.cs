using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.DbModel.Projections;

namespace Rnwood.Smtp4dev.Tests.TestHelpers
{
    internal class TestMessagesRepository : IMessagesRepository
    {
        private readonly List<Message> messages = new List<Message>();

        public Smtp4devDbContext DbContext => null;

        public Task AddMessage(Message message)
        {
            messages.Add(message);
            return Task.CompletedTask;
        }

        public Task DeleteAllMessages(string mailbox)
        {
            messages.Clear();
            return Task.CompletedTask;
        }

        public Task DeleteMessage(Guid id)
        {
            var m = messages.FirstOrDefault(x => x.Id == id);
            if (m != null) messages.Remove(m);
            return Task.CompletedTask;
        }

        public IQueryable<Message> GetAllMessages(bool unTracked = true) => messages.AsQueryable();

        public IQueryable<Message> GetMessages(string mailboxName, string folderName, bool unTracked = true) => messages.AsQueryable();

        public IQueryable<MessageSummaryProjection> GetMessageSummaries(string mailboxName, string folderName) => messages.Select(m => new MessageSummaryProjection { Id = m.Id, Subject = m.Subject }).AsQueryable();

        public Task<Message> TryGetMessageById(Guid id, bool tracked) => Task.FromResult(messages.FirstOrDefault(x => x.Id == id));

        public Task MarkAllMessagesRead(string mailbox)
        {
            foreach (var m in messages) m.IsUnread = false;
            return Task.CompletedTask;
        }

        public Task MarkMessageRead(Guid id)
        {
            var m = messages.FirstOrDefault(x => x.Id == id);
            if (m != null) m.IsUnread = false;
            return Task.CompletedTask;
        }
    }
}
