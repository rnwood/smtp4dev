using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Data
{
    public class Smtp4devDbContext : DbContext
    {
        public Smtp4devDbContext(DbContextOptions<Smtp4devDbContext> options)
        : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<Session> Sessions { get; set; }

        public DbSet<ImapState> ImapState { get; set; }
    }

}

