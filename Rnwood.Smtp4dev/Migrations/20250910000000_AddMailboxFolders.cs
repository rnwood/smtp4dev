using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    [Migration("20250910000000_AddMailboxFolders")]
    public partial class AddMailboxFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create MailboxFolders table first
            migrationBuilder.CreateTable(
                name: "MailboxFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    MailboxId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailboxFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailboxFolders_Mailboxes_MailboxId",
                        column: x => x.MailboxId,
                        principalTable: "Mailboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create index for MailboxId in MailboxFolders
            migrationBuilder.CreateIndex(
                name: "IX_MailboxFolders_MailboxId",
                table: "MailboxFolders",
                column: "MailboxId");

            // Add MailboxFolderId column to Messages table (nullable initially)
            migrationBuilder.AddColumn<Guid>(
                name: "MailboxFolderId",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            // Create index for MailboxFolderId
            migrationBuilder.CreateIndex(
                name: "IX_Messages_MailboxFolderId",
                table: "Messages",
                column: "MailboxFolderId");

            // Add foreign key relationship for Messages to MailboxFolders (with null handling)
            migrationBuilder.AddForeignKey(
                name: "FK_Messages_MailboxFolders_MailboxFolderId",
                table: "Messages",
                column: "MailboxFolderId",
                principalTable: "MailboxFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_MailboxFolders_MailboxFolderId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "MailboxFolders");

            migrationBuilder.DropIndex(
                name: "IX_Messages_MailboxFolderId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MailboxFolderId",
                table: "Messages");
        }
    }
}