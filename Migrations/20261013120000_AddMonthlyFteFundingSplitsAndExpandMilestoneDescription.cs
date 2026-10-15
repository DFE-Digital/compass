using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CompassDbContext))]
    [Migration("20261013120000_AddMonthlyFteFundingSplitsAndExpandMilestoneDescription")]
    public class AddMonthlyFteFundingSplitsAndExpandMilestoneDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyAdminFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyProgrammeFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyCapitalFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyOtherPermFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Milestones",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyAdminFte",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "MonthlyProgrammeFte",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "MonthlyCapitalFte",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "MonthlyOtherPermFte",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Milestones",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
