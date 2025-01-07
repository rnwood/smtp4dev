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

        public IQueryable<Folder> GetAllFolders()
        {
            var query = dbContext.Folders;
            return query;
        }

        public Smtp4devDbContext DbContext => this.dbContext;

       
        
    }
}