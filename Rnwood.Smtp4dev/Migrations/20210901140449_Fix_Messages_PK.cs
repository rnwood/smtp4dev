using Microsoft.EntityFrameworkCore.Migrations;

namespace Rnwood.Smtp4dev.Migrations
{
    public partial class Fix_Messages_PK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
PRAGMA foreign_keys = 0;

create table Messages_dg_tmp
(
	Id TEXT not null
	constraint PK_Messages
    	primary key,
	""From"" TEXT,
    ""To"" TEXT,
    ReceivedDate TEXT not null,
    Subject TEXT,
    Data BLOB,
    MimeParseError TEXT,
    SessionId TEXT,
    AttachmentCount INTEGER default 0 not null,
    IsUnread INTEGER default 0 not null,
    RelayError TEXT,
    ImapUid INTEGER not null,
    SecureConnection INTEGER default 0 not null
);

insert into Messages_dg_tmp(Id, ""From"", ""To"", ReceivedDate, Subject, Data, MimeParseError, SessionId, AttachmentCount, IsUnread, RelayError, ImapUid, SecureConnection) 
    select Id, ""From"", ""To"", ReceivedDate, Subject, Data, MimeParseError, SessionId, AttachmentCount, IsUnread, RelayError, ImapUid, SecureConnection from Messages;

drop table Messages;

alter table Messages_dg_tmp rename to Messages;

create unique index IX_ID

on Messages(Id);

create index IX_Messages_SessionId
    on Messages(SessionId);

PRAGMA foreign_keys = 1;
"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
