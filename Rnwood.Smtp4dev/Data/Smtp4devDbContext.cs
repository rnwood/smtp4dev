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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            UtcDateTimeValueConverter.Apply(modelBuilder);

            modelBuilder.Entity<MessageRelay>()
                .HasOne(r => r.Message)
                .WithMany(x => x.Relays)
                .HasForeignKey(x => x.MessageId)
                .IsRequired();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<MessageRelay> MessageRelays { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Session> Sessions { get; set; }

        public DbSet<ImapState> ImapState { get; set; }
    }
}