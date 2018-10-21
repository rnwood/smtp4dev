using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rnwood.Smtp4dev.Migrations
{
    public partial class AddSessionStartData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        { 

            migrationBuilder.Sql(@"CREATE TABLE Sessions_temp AS SELECT * FROM Sessions");

            migrationBuilder.DropTable(
              name: "Sessions");

            migrationBuilder.CreateTable(
             name: "Sessions",
             columns: table => new
             {
                 Id = table.Column<Guid>(nullable: false),
                 Log = table.Column<string>(nullable: true),
                 NumberOfMessages = table.Column<int>(nullable: false),
                 ClientAddress = table.Column<string>(nullable: true),
                 ClientName = table.Column<string>(nullable: true),
                 EndDate = table.Column<DateTime>(nullable: true),
                 StartDate = table.Column<DateTime>(nullable: false)
             },
             constraints: table =>
             {
                 table.PrimaryKey("PK_Sessions", x => x.Id);
             });

            migrationBuilder.Sql(@"INSERT INTO Sessions SELECT *, EndDate as StartDate FROM Sessions_Temp");

            migrationBuilder.DropTable(
              name: "Sessions_temp");


        }
    }
}
