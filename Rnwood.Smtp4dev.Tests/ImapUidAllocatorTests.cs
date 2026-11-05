using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Xunit;

namespace Rnwood.Smtp4dev.Tests
{
    public class ImapUidAllocatorTests
    {
        [Fact]
        public async Task AllocatesUniqueUidsAcrossConcurrentContexts()
        {
            var connectionString = $"Data Source=file:imap-uid-{Guid.NewGuid():N}?mode=memory&cache=shared";
            using var anchorConnection = new SqliteConnection(connectionString);
            anchorConnection.Open();

            var options = new DbContextOptionsBuilder<Smtp4devDbContext>()
                .UseSqlite(connectionString)
                .Options;

            using (var setupContext = new Smtp4devDbContext(options))
            {
                setupContext.Database.Migrate();
                setupContext.ImapState.Single(state => state.Id == Guid.Empty).LastUid = 0;
                setupContext.SaveChanges();
            }

            const int allocationCount = 8;
            using var start = new Barrier(allocationCount);
            var allocations = Enumerable.Range(0, allocationCount)
                .Select(_ => Task.Factory.StartNew(() =>
                {
                    using var context = new Smtp4devDbContext(options);
                    context.Database.OpenConnection();
                    if (!start.SignalAndWait(TimeSpan.FromSeconds(30)))
                    {
                        throw new TimeoutException("Timed out waiting to start concurrent UID allocations.");
                    }

                    return ImapUidAllocator.AllocateNextUid(context);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default))
                .ToArray();

            var allocatedUids = await Task.WhenAll(allocations);

            Assert.Equal(Enumerable.Range(1, allocationCount).Select(uid => (long)uid), allocatedUids.OrderBy(uid => uid));
        }
    }
}
