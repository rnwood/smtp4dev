using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Data
{
    public static class ImapUidAllocator
    {
        private static readonly object SyncRoot = new();

        public static long AllocateNextUid(Smtp4devDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            lock (SyncRoot)
            {
                var ownsTransaction = dbContext.Database.CurrentTransaction == null;
                using var transaction = ownsTransaction ? dbContext.Database.BeginTransaction() : null;

                var stateEntry = dbContext.ChangeTracker.Entries<ImapState>()
                    .SingleOrDefault();
                if (stateEntry == null)
                {
                    var state = dbContext.ImapState.SingleOrDefault();
                    if (state == null)
                    {
                        state = new ImapState { Id = Guid.Empty, LastUid = 0 };
                        dbContext.ImapState.Add(state);
                        dbContext.SaveChanges();
                    }

                    stateEntry = dbContext.Entry(state);
                }

                var stateId = stateEntry.Entity.Id;
                var updatedRows = dbContext.ImapState
                    .Where(state => state.Id == stateId)
                    .ExecuteUpdate(setters => setters.SetProperty(
                        state => state.LastUid,
                        state => state.LastUid < 0 ? 1 : state.LastUid + 1));
                if (updatedRows != 1)
                {
                    throw new InvalidOperationException("The IMAP UID state row could not be updated.");
                }

                var lastUid = dbContext.ImapState
                    .AsNoTracking()
                    .Where(state => state.Id == stateId)
                    .Select(state => state.LastUid)
                    .Single();

                var lastUidProperty = stateEntry.Property(state => state.LastUid);
                lastUidProperty.CurrentValue = lastUid;
                lastUidProperty.OriginalValue = lastUid;
                lastUidProperty.IsModified = false;

                transaction?.Commit();
                return lastUid;
            }
        }

        public static long GetNextUid(Smtp4devDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            var lastUid = dbContext.ImapState
                .AsNoTracking()
                .Select(state => state.LastUid)
                .SingleOrDefault();
            return Math.Max(0, lastUid) + 1;
        }
    }
}
