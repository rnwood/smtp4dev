using System;
using System.Linq;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Xunit;

namespace Rnwood.Smtp4dev.Tests
{
    public class StartupImapStateTests : IDisposable
    {
        private readonly SqliteInMemory sqliteForTesting = new();

        [Fact]
        public void EnsureImapStateInitializedPreservesHighWaterMarkWhenNoMessagesRemain()
        {
            using var context = new Smtp4devDbContext(sqliteForTesting.ContextOptions);
            context.ImapState.Single(state => state.Id == Guid.Empty).LastUid = 42;
            context.SaveChanges();

            Startup.EnsureImapStateInitialized(context);

            Assert.Equal(42, context.ImapState.Single().LastUid);
        }

        public void Dispose()
        {
            sqliteForTesting.Dispose();
        }
    }
}
