using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    public partial class AddMailboxFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create MailboxFolders table
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

            // Add MailboxFolderId column to Messages table
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

            // Create index for MailboxId in MailboxFolders
            migrationBuilder.CreateIndex(
                name: "IX_MailboxFolders_MailboxId",
                table: "MailboxFolders",
                column: "MailboxId");

            // Add foreign key relationship for Messages to MailboxFolders
            migrationBuilder.AddForeignKey(
                name: "FK_Messages_MailboxFolders_MailboxFolderId",
                table: "Messages",
                column: "MailboxFolderId",
                principalTable: "MailboxFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Data migration: Create default folders for existing mailboxes
            migrationBuilder.Sql(@"
                INSERT INTO MailboxFolders (Id, Name, MailboxId)
                SELECT 
                    lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(6))),
                    'INBOX',
                    Id
                FROM Mailboxes;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO MailboxFolders (Id, Name, MailboxId)
                SELECT 
                    lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(6))),
                    'Sent',
                    Id
                FROM Mailboxes;
            ");

            // Migrate existing messages to INBOX folders
            migrationBuilder.Sql(@"
                UPDATE Messages 
                SET MailboxFolderId = (
                    SELECT mf.Id 
                    FROM MailboxFolders mf 
                    WHERE mf.MailboxId = Messages.MailboxId 
                    AND mf.Name = 'INBOX'
                    LIMIT 1
                )
                WHERE MailboxId IS NOT NULL;
            ");
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