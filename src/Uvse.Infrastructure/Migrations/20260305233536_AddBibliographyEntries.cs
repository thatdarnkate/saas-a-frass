using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBibliographyEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bibliography_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedSummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Hyperlink = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SourceText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bibliography_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bibliography_entries_generated_summaries_GeneratedSummaryId",
                        column: x => x.GeneratedSummaryId,
                        principalTable: "generated_summaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bibliography_entries_GeneratedSummaryId_Position",
                table: "bibliography_entries",
                columns: new[] { "GeneratedSummaryId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bibliography_entries");
        }
    }
}
