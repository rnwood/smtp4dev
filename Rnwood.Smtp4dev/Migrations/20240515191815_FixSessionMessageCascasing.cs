using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    public partial class FixSessionMessageCascasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Sessions_SessionId",
                table: "Messages");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Sessions_SessionId",
                table: "Messages",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Sessions_SessionId",
                table: "Messages");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Sessions_SessionId",
                table: "Messages",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id");
        }
    }
}
