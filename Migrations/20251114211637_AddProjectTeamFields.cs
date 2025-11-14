using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTeamFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "ProjectContacts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Permanent");

            migrationBuilder.AddColumn<string>(
                name: "FundingArrangement",
                table: "ProjectContacts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Not specified");

            migrationBuilder.AddColumn<string>(
                name: "LeaveReason",
                table: "ProjectContacts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftAt",
                table: "ProjectContacts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamStatus",
                table: "ProjectContacts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "current");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ProjectContacts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContacts_TeamStatus",
                table: "ProjectContacts",
                column: "TeamStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContacts_UserId",
                table: "ProjectContacts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectContacts_Users_UserId",
                table: "ProjectContacts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectContacts_Users_UserId",
                table: "ProjectContacts");

            migrationBuilder.DropIndex(
                name: "IX_ProjectContacts_TeamStatus",
                table: "ProjectContacts");

            migrationBuilder.DropIndex(
                name: "IX_ProjectContacts_UserId",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "FundingArrangement",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "LeaveReason",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "LeftAt",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "TeamStatus",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProjectContacts");
        }
    }
}
