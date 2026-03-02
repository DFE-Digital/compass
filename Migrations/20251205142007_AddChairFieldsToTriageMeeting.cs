using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddChairFieldsToTriageMeeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChairEmail",
                table: "TriageMeetings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChairName",
                table: "TriageMeetings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChairObjectId",
                table: "TriageMeetings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCost",
                table: "TrainingRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "TrainingRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedDate",
                table: "TrainingRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrainingCompleted",
                table: "TrainingRequests",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChairEmail",
                table: "TriageMeetings");

            migrationBuilder.DropColumn(
                name: "ChairName",
                table: "TriageMeetings");

            migrationBuilder.DropColumn(
                name: "ChairObjectId",
                table: "TriageMeetings");

            migrationBuilder.DropColumn(
                name: "ActualCost",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "PlannedDate",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "TrainingCompleted",
                table: "TrainingRequests");
        }
    }
}
