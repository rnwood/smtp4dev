using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    public partial class NET10Upgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HasWarnings",
                table: "Sessions",
                newName: "HasBareLineFeed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HasBareLineFeed",
                table: "Sessions",
                newName: "HasWarnings");
        }
    }
}
