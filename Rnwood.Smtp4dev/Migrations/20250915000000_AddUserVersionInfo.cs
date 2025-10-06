using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVersionInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserVersionInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    LastSeenVersion = table.Column<string>(type: "TEXT", nullable: true),
                    LastCheckedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WhatsNewDismissed = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdateNotificationDismissed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVersionInfos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserVersionInfos");
        }
    }
}
