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
                .UseSqlite($"Data Source=file:cachedb{Guid.NewGuid()}?mode=memory&cache=shared")
                .Options;
            _connection = RelationalOptionsExtension.Extract(ContextOptions).Connection;
            using var context = new Smtp4devDbContext(ContextOptions);
            context.Database.Migrate();
        }

        protected internal DbContextOptions<Smtp4devDbContext> ContextOptions { get; }

  

        public void Dispose() => _connection.Dispose();
    }
}