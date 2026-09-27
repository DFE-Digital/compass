using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260927170000_SharedCensusThemesInheritance")]
    public partial class SharedCensusThemesInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "CoreCensusThemeFields",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReportable",
                table: "CoreCensusThemeFields",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalAttributeKey",
                table: "CoreCensusThemeFields",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationPattern",
                table: "CoreCensusThemeFields",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinNumber",
                table: "CoreCensusThemeFields",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxNumber",
                table: "CoreCensusThemeFields",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldStableKey",
                table: "ServiceDataModelAnswers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldStableKey",
                table: "ServiceDataModelProposedRegisterChanges",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Backfill stable keys from copied fields so existing answers survive the switch to inheritance.
            migrationBuilder.Sql("""
                UPDATE a
                SET a.FieldStableKey = f.StableKey
                FROM ServiceDataModelAnswers a
                INNER JOIN ServiceDataModelFields f ON f.Id = a.ServiceDataModelFieldId
                WHERE a.FieldStableKey IS NULL AND a.ServiceDataModelFieldId IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE p
                SET p.FieldStableKey = f.StableKey
                FROM ServiceDataModelProposedRegisterChanges p
                INNER JOIN ServiceDataModelFields f ON f.Id = p.ServiceDataModelFieldId
                WHERE p.FieldStableKey IS NULL AND p.ServiceDataModelFieldId IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ServiceDataModelAnswers_ServiceDataModelSubmissionId_ServiceDataModelFieldId",
                table: "ServiceDataModelAnswers");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceDataModelFieldId",
                table: "ServiceDataModelAnswers",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceDataModelFieldId",
                table: "ServiceDataModelProposedRegisterChanges",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAnswers_ServiceDataModelSubmissionId_FieldStableKey",
                table: "ServiceDataModelAnswers",
                columns: new[] { "ServiceDataModelSubmissionId", "FieldStableKey" });

            // Keep IX_ServiceDataModelAnswers_ServiceDataModelFieldId from AddServiceDataModels
            // (AlterColumn to nullable does not drop it on SQL Server).

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelProposedRegisterChanges_FieldStableKey",
                table: "ServiceDataModelProposedRegisterChanges",
                column: "FieldStableKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceDataModelAnswers_ServiceDataModelSubmissionId_FieldStableKey",
                table: "ServiceDataModelAnswers");

            migrationBuilder.DropIndex(
                name: "IX_ServiceDataModelProposedRegisterChanges_FieldStableKey",
                table: "ServiceDataModelProposedRegisterChanges");

            migrationBuilder.DropColumn(
                name: "FieldStableKey",
                table: "ServiceDataModelAnswers");

            migrationBuilder.DropColumn(
                name: "FieldStableKey",
                table: "ServiceDataModelProposedRegisterChanges");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "CoreCensusThemeFields");

            migrationBuilder.DropColumn(
                name: "IsReportable",
                table: "CoreCensusThemeFields");

            migrationBuilder.DropColumn(
                name: "CanonicalAttributeKey",
                table: "CoreCensusThemeFields");

            migrationBuilder.DropColumn(
                name: "ValidationPattern",
                table: "CoreCensusThemeFields");

            migrationBuilder.DropColumn(
                name: "MinNumber",
                table: "CoreCensusThemeFields");

            migrationBuilder.DropColumn(
                name: "MaxNumber",
                table: "CoreCensusThemeFields");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceDataModelFieldId",
                table: "ServiceDataModelAnswers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceDataModelFieldId",
                table: "ServiceDataModelProposedRegisterChanges",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAnswers_ServiceDataModelSubmissionId_ServiceDataModelFieldId",
                table: "ServiceDataModelAnswers",
                columns: new[] { "ServiceDataModelSubmissionId", "ServiceDataModelFieldId" },
                unique: true);
        }
    }
}
