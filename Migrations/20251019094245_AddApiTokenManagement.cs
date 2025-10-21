using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddApiTokenManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByEmail = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiTokenId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HttpMethod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    RequestPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    QueryString = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RequestBody = table.Column<string>(type: "text", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiRequestLogs_ApiTokens_ApiTokenId",
                        column: x => x.ApiTokenId,
                        principalTable: "ApiTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiTokenPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiTokenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Resource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CanRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanCreate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDelete = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokenPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiTokenPermissions_ApiTokens_ApiTokenId",
                        column: x => x.ApiTokenId,
                        principalTable: "ApiTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_ApiTokenId",
                table: "ApiRequestLogs",
                column: "ApiTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_IsSuccess",
                table: "ApiRequestLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_RequestTimestamp",
                table: "ApiRequestLogs",
                column: "RequestTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokenPermissions_ApiTokenId_Resource",
                table: "ApiTokenPermissions",
                columns: new[] { "ApiTokenId", "Resource" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_CreatedAt",
                table: "ApiTokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_ExpiresAt",
                table: "ApiTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_IsActive",
                table: "ApiTokens",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_Token",
                table: "ApiTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiRequestLogs");

            migrationBuilder.DropTable(
                name: "ApiTokenPermissions");

            migrationBuilder.DropTable(
                name: "ApiTokens");
        }
    }
}
