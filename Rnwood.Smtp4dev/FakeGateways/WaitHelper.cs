#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.FakeGateways
{
    public static class WaitHelper
    {
        public const int DefaultTimeoutSeconds = 30;
        public const int MaxTimeoutSeconds = 120;

        // Safety net in case a change notification is missed.
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Returns the oldest item matching the predicate, waiting for store changes until the timeout.
        /// Returns null on timeout. Throws OperationCanceledException when the token is cancelled.
        /// </summary>
        public static async Task<T?> WaitForOldestAsync<T>(RingBufferStore<T> store, Func<T, bool> predicate, TimeSpan timeout, CancellationToken cancellationToken) where T : class, IStoredItem
        {
            DateTime deadline = DateTime.UtcNow + timeout;
            var signal = new SemaphoreSlim(0, int.MaxValue);
            Action handler = () => { try { signal.Release(); } catch (ObjectDisposedException) { } };
            store.Changed += handler;
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    T? found = store.Query(predicate).FirstOrDefault();
                    if (found != null)
                    {
                        return found;
                    }

                    TimeSpan remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                    {
                        return null;
                    }

                    await signal.WaitAsync(remaining < PollInterval ? remaining : PollInterval, cancellationToken);
                }
            }
            finally
            {
                store.Changed -= handler;
                signal.Dispose();
            }
        }
    }
}
