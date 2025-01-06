using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Data
{
    public interface IMailboxRepository
    {
        Task CreateMailbox(string mailbox);
        
        IQueryable<DbModel.Mailbox> GetAllMailboxes();

        Smtp4devDbContext DbContext { get; }
    }
}