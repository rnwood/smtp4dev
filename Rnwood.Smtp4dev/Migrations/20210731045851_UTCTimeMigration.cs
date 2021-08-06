using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rnwood.Smtp4dev.Migrations
{
    public partial class UTCTimeMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateUpdateToUtcRawSql());
        }

        public static string CreateUpdateToUtcRawSql()
        {
            TimeZoneInfo tz = TimeZoneInfo.Local;
            var offset = tz.GetUtcOffset(DateTime.Now.ToLocalTime());
            return @$"
Update Messages
set ReceivedDate = DATETIME(ReceivedDate, '{-offset.TotalMinutes} minutes');

Update Sessions
set StartDate = DATETIME(StartDate, '{-offset.TotalMinutes} minutes'),
EndDate = DATETIME(EndDate, '{-offset.TotalMinutes} minutes');
";
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
