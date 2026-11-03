using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueMailboxFolderNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE Messages
SET MailboxFolderId = (
    SELECT MIN(canonical.Id)
    FROM MailboxFolders AS duplicate
    JOIN MailboxFolders AS canonical
        ON canonical.MailboxId = duplicate.MailboxId
        AND canonical.Name = duplicate.Name
    WHERE duplicate.Id = Messages.MailboxFolderId
)
WHERE MailboxFolderId IN (
    SELECT duplicate.Id
    FROM MailboxFolders AS duplicate
    WHERE EXISTS (
        SELECT 1
        FROM MailboxFolders AS canonical
        WHERE canonical.MailboxId = duplicate.MailboxId
            AND canonical.Name = duplicate.Name
            AND canonical.Id < duplicate.Id
    )
);
DELETE FROM MailboxFolders
WHERE Id IN (
    SELECT duplicate.Id
    FROM MailboxFolders AS duplicate
    WHERE EXISTS (
        SELECT 1
        FROM MailboxFolders AS canonical
        WHERE canonical.MailboxId = duplicate.MailboxId
            AND canonical.Name = duplicate.Name
            AND canonical.Id < duplicate.Id
    )
);");

            migrationBuilder.DropIndex(
                name: "IX_MailboxFolders_MailboxId",
                table: "MailboxFolders");

            migrationBuilder.CreateIndex(
                name: "IX_MailboxFolders_MailboxId_Name",
                table: "MailboxFolders",
                columns: new[] { "MailboxId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MailboxFolders_MailboxId_Name",
                table: "MailboxFolders");

            migrationBuilder.CreateIndex(
                name: "IX_MailboxFolders_MailboxId",
                table: "MailboxFolders",
                column: "MailboxId");
        }
    }
}
