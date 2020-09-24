using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rnwood.Smtp4dev.Migrations
{
    public partial class AddImapState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"PRAGMA foreign_keys = 0;

CREATE TABLE MessagesTemp AS SELECT *
                                          FROM Messages;

DROP TABLE Messages;

CREATE TABLE Messages (
    Id              TEXT    NOT NULL,
    [From]          TEXT,
    [To]            TEXT,
    ReceivedDate    TEXT    NOT NULL,
    Subject         TEXT,
    Data            BLOB,
    MimeParseError  TEXT,
    SessionId       TEXT,
    AttachmentCount INTEGER NOT NULL
                            DEFAULT 0,
    IsUnread        INTEGER NOT NULL
                            DEFAULT 0,
    RelayError      TEXT,
    ImapUid         INTEGER NOT NULL
);

INSERT INTO Messages (
                         Id,
                         [From],
                         [To],
                         ReceivedDate,
                         Subject,
                         Data,
                         MimeParseError,
                         SessionId,
                         AttachmentCount,
                         IsUnread,
                         RelayError,
                         ImapUid
                     )
                     SELECT Id,
                            ""From"",
                            ""To"",
                            ReceivedDate,
                            Subject,
                            Data,
                            MimeParseError,
                            SessionId,
                            AttachmentCount,
                            IsUnread,
                            RelayError,
                            (SELECT COUNT(*)+1 FROM MessagesTemp om WHERE om.ReceivedDate < MessagesTemp.ReceivedDate)
                            
                       FROM MessagesTemp;

            DROP TABLE MessagesTemp;

            CREATE INDEX IX_Messages_SessionId ON Messages(
                ""SessionId""
            );

            PRAGMA foreign_keys = 1;");

            migrationBuilder.CreateTable(
                name: "ImapState",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    LastUid = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImapState", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO ImapState (Id, LastUid) select '00000000-0000-0000-0000-000000000000', ifnull(max(ImapUid),1) FROM Messages");
        }

    }
}
