using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Rnwood.Smtp4dev.Data;

namespace Rnwood.Smtp4dev.Tests.DBMigrations.Helpers
{
    public class SqliteInMemory : IDisposable
    {
        private readonly DbConnection _connection;

        public SqliteInMemory()
        {
            this.ContextOptions = new DbContextOptionsBuilder<Smtp4devDbContext>()
                .UseSqlite(CreateInMemoryDatabase())
                .Options;
            _connection = RelationalOptionsExtension.Extract(ContextOptions).Connection;
            using var context = new Smtp4devDbContext(ContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.SaveChanges();
        }

        protected internal DbContextOptions<Smtp4devDbContext> ContextOptions { get; }

        private DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            return connection;
        }

        public void Dispose() => _connection.Dispose();
    }
}