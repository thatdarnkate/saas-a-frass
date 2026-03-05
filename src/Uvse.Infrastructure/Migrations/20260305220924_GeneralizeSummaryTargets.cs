using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uvse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeSummaryTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_generated_summaries_TenantId_ProviderKey_RequestedByUserId_~",
                table: "generated_summaries");

            migrationBuilder.AddColumn<Guid>(
                name: "ComparisonSummaryId",
                table: "generated_summaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DatasourceId",
                table: "generated_summaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "generated_summaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedModes",
                table: "generated_summaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetType",
                table: "generated_summaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_generated_summaries_TargetType_ProjectId_DatasourceId_Creat~",
                table: "generated_summaries",
                columns: new[] { "TargetType", "ProjectId", "DatasourceId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_generated_summaries_TenantId_RequestedByUserId_TargetType_P~",
                table: "generated_summaries",
                columns: new[] { "TenantId", "RequestedByUserId", "TargetType", "ProjectId", "DatasourceId", "FromUtc", "ToUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_generated_summaries_TargetType_ProjectId_DatasourceId_Creat~",
                table: "generated_summaries");

            migrationBuilder.DropIndex(
                name: "IX_generated_summaries_TenantId_RequestedByUserId_TargetType_P~",
                table: "generated_summaries");

            migrationBuilder.DropColumn(
                name: "ComparisonSummaryId",
                table: "generated_summaries");

            migrationBuilder.DropColumn(
                name: "DatasourceId",
                table: "generated_summaries");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "generated_summaries");

            migrationBuilder.DropColumn(
                name: "RequestedModes",
                table: "generated_summaries");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "generated_summaries");

            migrationBuilder.CreateIndex(
                name: "IX_generated_summaries_TenantId_ProviderKey_RequestedByUserId_~",
                table: "generated_summaries",
                columns: new[] { "TenantId", "ProviderKey", "RequestedByUserId", "DetailLevel", "AudienceTone", "FromUtc", "ToUtc" },
                unique: true);
        }
    }
}
