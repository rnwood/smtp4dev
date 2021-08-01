using System;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Migrations;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.DBMigrations
{
    public class TimezoneConvertMigrationTests
    {
        private readonly SqliteInMemory _sqlLiteForTesting;

        public TimezoneConvertMigrationTests()
        {
            _sqlLiteForTesting = new SqliteInMemory();
        }

        [Fact]
        public void DatesAreStoredInUtcAndKindIsSetOnRetrieval()
        {
            var timeZone = GetTestTimezone();
            var testDate = new DateTime(2021, 1, 4, 10, 0, 0, DateTimeKind.Utc);

            using (new FakeLocalTimeZone(GetTimeZoneInfo(timeZone)))
            {
                using (var context = new Smtp4devDbContext(_sqlLiteForTesting.ContextOptions))
                {
                    context.Messages.Add(new Message {From = "test"});
                    context.SaveChanges();

                    //manually set time so we don't go through any EF converts.
                    var sql = @$"
UPDATE Messages
SET ReceivedDate = DATETIME('{testDate:yyyy-MM-dd HH:mm:ss}');
";
                    context.Database.ExecuteSqlRaw(sql);
                    context.ChangeTracker.Clear();

                    // verify entity return value
                    var message = context.Messages.Single(x => x.From == "test");
                    message.ReceivedDate.Kind.Should().Be(DateTimeKind.Utc);
                    message.ReceivedDate.ToLocalTime().Should().Be(new DateTime(testDate.Year, testDate.Month, testDate.Day,
                        testDate.Hour + timeZone.Offset, 0, 0, DateTimeKind.Local));
                }
            }
        }

        [Fact]
        public void MigrationScriptMaintainsCorrectLocalTimeAfterCoversionTest()
        {
            var timeZone = GetTestTimezone();
            var testDate = new DateTime(2021, 1, 4, 10, 0, 0, DateTimeKind.Utc);

            using (new FakeLocalTimeZone(GetTimeZoneInfo(timeZone)))
            {
                using var context = new Smtp4devDbContext(_sqlLiteForTesting.ContextOptions);
                context.Messages.Add(new Message {From = "test"});
                context.Sessions.Add(new Session {Id = Guid.NewGuid(), Log = "Log"});
                context.SaveChanges();

                //manually set time so we don't go through any EF converts.  This will be the date pre migration (local time)
                var sql = @$"
UPDATE Messages
SET ReceivedDate = DATETIME('{testDate:yyyy-MM-dd HH:mm:ss}');
";
                context.Database.ExecuteSqlRaw(sql);
                context.ChangeTracker.Clear();

                // Run Migration Script SQL to convert to UTC.
                context.Database.ExecuteSqlRaw(UTCTimeMigration.CreateUpdateToUtcRawSql());

                // verify entity return value is correct when retrieved for actual timezone.
                var message = context.Messages.Single(x => x.From == "test");
                message.Should().NotBeNull();
                message.ReceivedDate.Kind.Should().Be(DateTimeKind.Utc);
                message.ReceivedDate.ToLocalTime().Should().Be(new DateTime(testDate.Year, testDate.Month, testDate.Day,
                    testDate.Hour, 0, 0, DateTimeKind.Local));
            }
        }

        private TimeZoneOffset GetTestTimezone()
        {
            return new TimeZoneOffset
            {
                Id = "Central America Standard Time", Offset = -6, SerializedTimeZone =
                    "Central America Standard Time;-360;(UTC-06:00) Central America;Central America Standard Time;Central America Summer Time;;"
            };
        }

        private TimeZoneInfo GetTimeZoneInfo(TimeZoneOffset timeZoneOffset)
        {
            // See issue with crossPlatform TimeZoneById calls https://stackoverflow.com/questions/41566395/timezoneinfo-in-net-core-when-hosting-on-unix-nginx
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneOffset.Id);
            return TimeZoneInfo.FromSerializedString(timeZoneOffset.SerializedTimeZone);
        }
    }
}