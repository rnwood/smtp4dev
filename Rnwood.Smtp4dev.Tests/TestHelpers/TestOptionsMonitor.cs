using System;
using Microsoft.Extensions.Options;

namespace Rnwood.Smtp4dev.Tests.TestHelpers
{
    public class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class, new()
    {
        public TestOptionsMonitor(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; private set; }

        public T Get(string name) => CurrentValue;

        public IDisposable OnChange(Action<T, string> listener) => new DummyDisposable();

        private class DummyDisposable : IDisposable { public void Dispose() { } }
    }
}
