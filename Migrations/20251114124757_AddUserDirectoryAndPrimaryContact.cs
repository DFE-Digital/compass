using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDirectoryAndPrimaryContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AzureObjectId",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Photo",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhotoUpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserPrincipalName",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryContactUserId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AzureObjectId",
                table: "Users",
                column: "AzureObjectId",
                unique: true,
                filter: "[AzureObjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PrimaryContactUserId",
                table: "Projects",
                column: "PrimaryContactUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_PrimaryContactUserId",
                table: "Projects",
                column: "PrimaryContactUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_PrimaryContactUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Users_AzureObjectId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Projects_PrimaryContactUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AzureObjectId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Photo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoUpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserPrincipalName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrimaryContactUserId",
                table: "Projects");
        }
    }
}
