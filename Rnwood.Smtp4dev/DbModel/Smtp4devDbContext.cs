using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.DbModel
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

