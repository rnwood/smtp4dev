#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>An item that can be kept in <see cref="RingBufferStore{T}"/>.</summary>
    public interface IStoredItem
    {
        string Id { get; }
        DateTime ReceivedAt { get; }
    }

    /// <summary>
    /// Thread-safe in-memory store keeping at most <c>capacity</c> newest items.
    /// Raises <see cref="Changed"/> after every modification (outside of the lock).
    /// </summary>
    public class RingBufferStore<T> where T : class, IStoredItem
    {
        private readonly object sync = new object();
        private readonly LinkedList<T> items = new LinkedList<T>();
        private readonly int capacity;

        public RingBufferStore(int capacity)
        {
            this.capacity = Math.Max(1, capacity);
        }

        /// <summary>Raised after items were added or removed.</summary>
        public event Action? Changed;

        public int Capacity => capacity;

        public void Add(T item)
        {
            lock (sync)
            {
                items.AddLast(item);
                while (items.Count > capacity)
                {
                    items.RemoveFirst();
                }
            }

            RaiseChanged();
        }

        /// <summary>Adds several items at once and raises <see cref="Changed"/> only once.</summary>
        public void AddRange(IEnumerable<T> newItems)
        {
            bool any = false;
            lock (sync)
            {
                foreach (T item in newItems)
                {
                    items.AddLast(item);
                    any = true;
                }
                while (items.Count > capacity)
                {
                    items.RemoveFirst();
                }
            }

            if (any)
            {
                RaiseChanged();
            }
        }

        /// <summary>Returns items matching the predicate, oldest first.</summary>
        public IReadOnlyList<T> Query(Func<T, bool>? predicate = null)
        {
            lock (sync)
            {
                return (predicate == null ? items : items.Where(predicate)).ToList();
            }
        }

        public T? Get(string id)
        {
            lock (sync)
            {
                return items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.Ordinal));
            }
        }

        public bool Remove(string id)
        {
            bool removed = false;
            lock (sync)
            {
                var node = items.First;
                while (node != null)
                {
                    if (string.Equals(node.Value.Id, id, StringComparison.Ordinal))
                    {
                        items.Remove(node);
                        removed = true;
                        break;
                    }
                    node = node.Next;
                }
            }

            if (removed)
            {
                RaiseChanged();
            }
            return removed;
        }

        /// <summary>Removes items matching the predicate (all when null) and returns how many were removed.</summary>
        public int RemoveWhere(Func<T, bool>? predicate = null)
        {
            int count = 0;
            lock (sync)
            {
                var node = items.First;
                while (node != null)
                {
                    var next = node.Next;
                    if (predicate == null || predicate(node.Value))
                    {
                        items.Remove(node);
                        count++;
                    }
                    node = next;
                }
            }

            if (count > 0)
            {
                RaiseChanged();
            }
            return count;
        }

        private void RaiseChanged()
        {
            var handlers = Changed;
            if (handlers == null)
            {
                return;
            }

            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch
                {
                    // A faulty listener must not break request processing.
                }
            }
        }
    }
}
