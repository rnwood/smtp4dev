using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    [Migration("20250908191900_AddBareLineFeedDetection")]
    public partial class AddBareLineFeedDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasBareLineFeed",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasBareLineFeed",
                table: "Messages");
        }
    }
}