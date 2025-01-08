using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Data
{
    public class FolderRepository : IFolderRepository
    {
        private readonly ITaskQueue taskQueue;
        private readonly NotificationsHub notificationsHub;
        private readonly Smtp4devDbContext dbContext;

        public FolderRepository(ITaskQueue taskQueue, NotificationsHub notificationsHub, Smtp4devDbContext dbContext)
        {
            this.taskQueue = taskQueue;
            this.notificationsHub = notificationsHub;
            this.dbContext = dbContext;
        }

        public Task CreateFolder(string name, Mailbox mailbox)
        {
            return taskQueue.QueueTask(() =>
            {
                dbContext.Folders.Add(new Folder()
                {
                    Name = name,
                    Path = name,
                    Mailbox = mailbox,
                    Id = Guid.NewGuid(),
                });
                dbContext.SaveChanges();
            }, true);
        }

        public IQueryable<Folder> GetAllFolders(Mailbox mailbox)
        {
            return dbContext.Folders.Where(f => f.Mailbox == mailbox);
        }

        public Smtp4devDbContext DbContext => this.dbContext;
        public Folder GetFolderOrCreate(string folderName, Mailbox mailbox)
        {
            var folder =  dbContext.Folders.Include(m => m.Mailbox).FirstOrDefault(m => m.Path == folderName);
            if (folder == null)
            {
                folder = new Folder()
                {
                    Name = folderName,
                    Path = folderName,
                    Mailbox = mailbox,
                    Id = Guid.NewGuid(),
                };
                
                dbContext.Folders.Add(folder);
            }

            return folder;
        }
    }
}