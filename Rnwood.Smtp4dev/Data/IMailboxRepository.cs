using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Data
{
    public interface IMailboxRepository
    {
        IQueryable<DbModel.Mailbox> GetAllMailboxes();
        
        Mailbox GetMailboxByName(string name);

        Smtp4devDbContext DbContext { get; }
    }
}