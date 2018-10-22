using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rnwood.Smtp4dev.Migrations
{
    public partial class AddSessionErrorInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.AddColumn<string>(
                name: "SessionError",
                table: "Sessions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionErrorType",
                table: "Sessions",
                nullable: true);

            
        }
        
    }
}
