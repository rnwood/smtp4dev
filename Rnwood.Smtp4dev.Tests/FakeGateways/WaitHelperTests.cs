using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class WaitHelperTests
    {
        [Fact]
        public async Task ReturnsExistingMatchImmediately_OldestFirst()
        {
            var store = new SmsStore(10);
            store.Add(TestData.Sms(id: "1"));
            store.Add(TestData.Sms(id: "2"));

            var found = await WaitHelper.WaitForOldestAsync(store, _ => true, TimeSpan.FromSeconds(5), CancellationToken.None);

            found.Id.Should().Be("1");
        }

        [Fact]
        public async Task WaitsForItemThatArrivesLater()
        {
            var store = new SmsStore(10);
            var task = WaitHelper.WaitForOldestAsync(store, i => i.To == "wanted", TimeSpan.FromSeconds(10), CancellationToken.None);

            await Task.Delay(100);
            store.Add(TestData.Sms(to: "other"));
            await Task.Delay(100);
            task.IsCompleted.Should().BeFalse();
            store.Add(TestData.Sms(to: "wanted", id: "w"));

            (await task.WaitAsync(TimeSpan.FromSeconds(5))).Id.Should().Be("w");
        }

        [Fact]
        public async Task ReturnsNullOnTimeout()
        {
            var store = new SmsStore(10);
            var sw = Stopwatch.StartNew();

            var found = await WaitHelper.WaitForOldestAsync(store, _ => true, TimeSpan.FromMilliseconds(300), CancellationToken.None);

            found.Should().BeNull();
            sw.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(250));
        }

        [Fact]
        public async Task ThrowsWhenCancelled()
        {
            var store = new SmsStore(10);
            using var cts = new CancellationTokenSource();
            var task = WaitHelper.WaitForOldestAsync(store, _ => true, TimeSpan.FromSeconds(30), cts.Token);

            cts.CancelAfter(100);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task UnsubscribesFromStoreAfterwards()
        {
            var store = new SmsStore(10);
            await WaitHelper.WaitForOldestAsync(store, _ => true, TimeSpan.FromMilliseconds(50), CancellationToken.None);

            // Would throw ObjectDisposedException inside the handler if it were still subscribed and not guarded - must simply not fail.
            store.Add(TestData.Sms());
        }
    }
}
