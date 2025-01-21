using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    public partial class AddFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    MailboxId = table.Column<Guid>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folder", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_FolderId",
                table: "Messages",
                column: "FolderId");
            
            migrationBuilder.CreateIndex(
                name: "IX_Mailbox_MailboxId",
                table: "Folders",
                column: "MailboxId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Folders_FolderId",
                table: "Messages",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Folders_FolderId",
                table: "Messages");
            
            migrationBuilder.DropForeignKey(
                name: "IX_Mailbox_MailboxId",
                table: "Folders");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_Messages_FolderId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Messages");
        }
    }
}
