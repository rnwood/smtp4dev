using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Data
{
    public interface IFolderRepository
    {
        Task CreateFolder(string name, Mailbox mailbox);
        
        IQueryable<DbModel.Folder> GetAllFolders();

        Smtp4devDbContext DbContext { get; }
    }
}