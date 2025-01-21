using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Namotion.Reflection;
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
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            modelBuilder.Entity<Mailbox>()
                .HasMany<Message>()
                    .WithOne(m => m.Mailbox)
                    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Folder>()
                .HasMany<Message>()
                .WithOne(m => m.Folder)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<Message>()
                .HasOne<Session>(x => x.Session)
               .WithMany()
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<MessageRelay> MessageRelays { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<ImapState> ImapState { get; set; }
        public DbSet<Mailbox> Mailboxes { get; set; }
        public DbSet<Folder> Folders { get; set; }
    }
}