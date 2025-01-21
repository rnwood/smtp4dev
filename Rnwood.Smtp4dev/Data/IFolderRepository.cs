using System.Linq;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Data
{
    public interface IFolderRepository
    {
        Task CreateFolder(string name, Mailbox mailbox);

        Task DeleteFolder(string folderName, Mailbox mailbox);


        IQueryable<DbModel.Folder> GetAllFolders(Mailbox mailbox);

        Smtp4devDbContext DbContext { get; }
        Folder GetFolderOrCreate(string folderName, Mailbox mailbox);
    }
}