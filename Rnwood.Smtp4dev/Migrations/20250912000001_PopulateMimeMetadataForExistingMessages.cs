using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using MimeKit;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;

#nullable disable

namespace Rnwood.Smtp4dev.Migrations
{
    /// <inheritdoc />
    [Migration("20250912000001_PopulateMimeMetadataForExistingMessages")]
    public partial class PopulateMimeMetadataForExistingMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration will be handled in code to populate existing messages
            // The actual population will be done in the application startup
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Clear the MIME metadata fields
            migrationBuilder.Sql("UPDATE Messages SET MimeMetadata = NULL, BodyText = NULL");
        }
    }
}