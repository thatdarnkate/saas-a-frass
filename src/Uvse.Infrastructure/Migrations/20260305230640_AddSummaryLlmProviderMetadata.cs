using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryLlmProviderMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LlmModel",
                table: "generated_summaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LlmProviderKey",
                table: "generated_summaries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LlmModel",
                table: "generated_summaries");

            migrationBuilder.DropColumn(
                name: "LlmProviderKey",
                table: "generated_summaries");
        }
    }
}
