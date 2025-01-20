using Rnwood.Smtp4dev.DbModel;
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

        IQueryable<DbModel.Message> GetMessages(string mailboxName, string folder = null, bool unTracked = true);

        Task AddMessage(DbModel.Message message);

        Task DeleteMessage(Guid id);

        Task DeleteAllMessages(string mailbox);

        Task<DbModel.Message> TryGetMessageById(Guid id, bool tracked);

        Task CopyMessageToFolder(Guid id, string targetFolder);
        Task TrimMessages();
        Task UpdateMessage(Message message);
    }
}