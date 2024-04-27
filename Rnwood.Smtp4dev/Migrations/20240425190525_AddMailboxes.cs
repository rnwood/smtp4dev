using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    public partial class AddMailboxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MailboxId",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Mailboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mailbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MailboxId",
                table: "Messages",
                column: "MailboxId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Mailboxes_MailboxId",
                table: "Messages",
                column: "MailboxId",
                principalTable: "Mailboxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Mailboxes_MailboxId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "Mailboxes");

            migrationBuilder.DropIndex(
                name: "IX_Messages_MailboxId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MailboxId",
                table: "Messages");
        }
    }
}
