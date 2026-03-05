using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "generated_summaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "text", nullable: false),
                    DetailLevel = table.Column<int>(type: "integer", nullable: false),
                    AudienceTone = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    FromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_summaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plugins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Domain = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    EncryptedSettingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    EnabledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_plugins", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_summaries_TenantId_ProviderKey_RequestedByUserId_D~",
                table: "generated_summaries",
                columns: new[] { "TenantId", "ProviderKey", "RequestedByUserId", "DetailLevel", "AudienceTone", "FromUtc", "ToUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_plugins_TenantId_ProviderKey",
                table: "tenant_plugins",
                columns: new[] { "TenantId", "ProviderKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_summaries");

            migrationBuilder.DropTable(
                name: "tenant_plugins");
        }
    }
}
