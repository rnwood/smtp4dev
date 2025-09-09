using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.DbModel.Projections;

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

        public Task DeleteMessage(Guid id)
        {
            Messages.RemoveAll(m => m.Id == id);
            return Task.CompletedTask;
        }

        public IQueryable<Message> GetAllMessages(bool unTracked = true)
        {
            return Messages.AsQueryable();
        }

        public IQueryable<Message> GetMessages(string mailboxName, bool unTracked = true)
        {
            return Messages.AsQueryable();
        }

        public IQueryable<MessageSummaryProjection> GetMessageSummaries(string mailboxName)
        {
            return Messages
                .Select(m => new MessageSummaryProjection()
                {
                    Id = m.Id,
                    From = m.From,
                    To = m.To,
                    Subject = m.Subject,
                    ReceivedDate = m.ReceivedDate,
                    AttachmentCount = m.AttachmentCount,
                    DeliveredTo = m.DeliveredTo,
                    IsRelayed = m.Relays.Count > 0,
                    IsUnread = m.IsUnread,
                    HasBareLineFeed = m.HasBareLineFeed
                }).AsQueryable();
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