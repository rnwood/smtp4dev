using System;
using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.DbModel.Projections;

namespace Rnwood.Smtp4dev.Data
{
    public interface IMessagesRepository
    {
        Task AddMessage(Message message);

        Task MarkAllMessagesRead(string mailbox);
        Task MarkMessageRead(Guid id);

        IQueryable<Message> GetAllMessages(bool unTracked = true);

        IQueryable<Message> GetMessages(string mailboxName, string folderName, bool unTracked = true);
        IQueryable<MessageSummaryProjection> GetMessageSummaries(string mailboxName, string folderName);

        Task DeleteMessage(Guid id);

        Task DeleteAllMessages(string mailbox);

        Task<Message> TryGetMessageById(Guid id, bool tracked);

        Smtp4devDbContext DbContext { get; }
    }
}