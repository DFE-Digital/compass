using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class WorkReportingPeriodExplicitDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ReportingCyclePeriods",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodEnd",
                table: "ReportingCyclePeriods",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStart",
                table: "ReportingCyclePeriods",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmissionCloses",
                table: "ReportingCyclePeriods",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmissionOpens",
                table: "ReportingCyclePeriods",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Backfill existing rows (PeriodKey like "2026-4"): derive calendar month bounds; use DueDate for submission window until edited in admin.
            migrationBuilder.Sql("""
UPDATE rp
SET
  PeriodStart = DATEFROMPARTS(
    TRY_CAST(LEFT(rp.PeriodKey, CHARINDEX('-', rp.PeriodKey) - 1) AS int),
    TRY_CAST(SUBSTRING(rp.PeriodKey, CHARINDEX('-', rp.PeriodKey) + 1, 10) AS int),
    1),
  PeriodEnd = EOMONTH(DATEFROMPARTS(
    TRY_CAST(LEFT(rp.PeriodKey, CHARINDEX('-', rp.PeriodKey) - 1) AS int),
    TRY_CAST(SUBSTRING(rp.PeriodKey, CHARINDEX('-', rp.PeriodKey) + 1, 10) AS int),
    1)),
  SubmissionOpens = CAST(rp.DueDate AS date),
  SubmissionCloses = CAST(rp.DueDate AS date),
  IsActive = 1
FROM ReportingCyclePeriods rp
WHERE rp.PeriodStart = '0001-01-01'
  AND CHARINDEX('-', rp.PeriodKey) > 0;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ReportingCyclePeriods");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "ReportingCyclePeriods");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "ReportingCyclePeriods");

            migrationBuilder.DropColumn(
                name: "SubmissionCloses",
                table: "ReportingCyclePeriods");

            migrationBuilder.DropColumn(
                name: "SubmissionOpens",
                table: "ReportingCyclePeriods");
        }
    }
}
