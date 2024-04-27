using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Data
{
    public interface IMessagesRepository
    {
        Task MarkAllMessagesRead(string mailbox);
        Task MarkMessageRead(Guid id);

        IQueryable<DbModel.Message> GetAllMessages(bool unTracked = true);

        IQueryable<DbModel.Message> GetMessages(string mailboxName, bool unTracked = true);

        Task DeleteMessage(Guid id);

        Task DeleteAllMessages(string mailbox);

        Task<DbModel.Message> TryGetMessageById(Guid id, bool tracked);

        Smtp4devDbContext DbContext { get; }
    }
}