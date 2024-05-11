using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveredTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveredTo",
                table: "Messages",
                type: "TEXT",
                nullable: true);

         }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredTo",
                table: "Messages");

         }
    }
}
