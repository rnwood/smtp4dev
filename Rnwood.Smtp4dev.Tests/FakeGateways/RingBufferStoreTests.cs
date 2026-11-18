using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class RingBufferStoreTests
    {
        [Fact]
        public void DropsOldestWhenCapacityExceeded()
        {
            var store = new SmsStore(3);
            for (int i = 1; i <= 5; i++) store.Add(TestData.Sms(id: i.ToString()));

            store.Query().Select(i => i.Id).Should().Equal("3", "4", "5");
        }

        [Fact]
        public void QueryFiltersAndKeepsOrderOldestFirst()
        {
            var store = new SmsStore(10);
            store.Add(TestData.Sms(to: "a", id: "1"));
            store.Add(TestData.Sms(to: "b", id: "2"));
            store.Add(TestData.Sms(to: "a", id: "3"));

            store.Query(i => i.To == "a").Select(i => i.Id).Should().Equal("1", "3");
        }

        [Fact]
        public void GetByIdAndRemove()
        {
            var store = new SmsStore(10);
            store.Add(TestData.Sms(id: "1"));

            store.Get("1").Should().NotBeNull();
            store.Get("x").Should().BeNull();
            store.Remove("x").Should().BeFalse();
            store.Remove("1").Should().BeTrue();
            store.Get("1").Should().BeNull();
        }

        [Fact]
        public void RemoveWhere_WithPredicateAndWithout()
        {
            var store = new SmsStore(10);
            store.Add(TestData.Sms(to: "a", id: "1"));
            store.Add(TestData.Sms(to: "b", id: "2"));
            store.Add(TestData.Sms(to: "a", id: "3"));

            store.RemoveWhere(i => i.To == "a").Should().Be(2);
            store.Query().Select(i => i.Id).Should().Equal("2");
            store.RemoveWhere().Should().Be(1);
            store.Query().Should().BeEmpty();
        }

        [Fact]
        public void ChangedIsRaisedForAddRemoveAndClear_ButNotForNoop()
        {
            var store = new SmsStore(10);
            int changes = 0;
            store.Changed += () => changes++;

            store.Add(TestData.Sms(id: "1"));
            store.Remove("1");
            store.Remove("1");
            store.Add(TestData.Sms(id: "2"));
            store.RemoveWhere();
            store.RemoveWhere();

            changes.Should().Be(4);
        }

        [Fact]
        public void AddRange_RaisesChangedOnceAndRespectsCapacity()
        {
            var store = new SmsStore(3);
            int changes = 0;
            store.Changed += () => changes++;

            store.AddRange(Enumerable.Range(1, 5).Select(i => TestData.Sms(id: i.ToString())).ToList());
            store.AddRange(new SmsMessage[0]);

            changes.Should().Be(1);
            store.Query().Select(i => i.Id).Should().Equal("3", "4", "5");
        }

        [Fact]
        public void FaultyListenerDoesNotBreakOthers()
        {
            var store = new SmsStore(10);
            int calls = 0;
            store.Changed += () => throw new InvalidOperationException();
            store.Changed += () => calls++;

            store.Add(TestData.Sms());

            calls.Should().Be(1);
        }

        [Fact]
        public async Task ConcurrentAddsAreThreadSafe()
        {
            var store = new SmsStore(50);
            await Task.WhenAll(Enumerable.Range(0, 8).Select(t => Task.Run(() =>
            {
                for (int i = 0; i < 500; i++)
                {
                    store.Add(TestData.Sms());
                    store.Query();
                }
            })));

            store.Query().Should().HaveCount(50);
        }
    }
}
