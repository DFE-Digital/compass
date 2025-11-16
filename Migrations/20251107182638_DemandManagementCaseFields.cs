using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class DemandManagementCaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDate",
                table: "DemandRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusChangeReason",
                table: "DemandRequests",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "StatusChangeReason",
                table: "DemandRequests");
        }
    }
}
