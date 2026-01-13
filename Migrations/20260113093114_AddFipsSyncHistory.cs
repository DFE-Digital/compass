using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFipsSyncHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FipsSyncHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SourceEnvironment = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetEnvironment = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    InitiatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProductsCreated = table.Column<int>(type: "int", nullable: false),
                    ProductsUpdated = table.Column<int>(type: "int", nullable: false),
                    ProductsSkipped = table.Column<int>(type: "int", nullable: false),
                    AssurancesSynced = table.Column<int>(type: "int", nullable: false),
                    AccessibilitySynced = table.Column<int>(type: "int", nullable: false),
                    ErrorsEncountered = table.Column<int>(type: "int", nullable: false),
                    ActionsLog = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Configuration = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsSyncHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FipsSyncHistories_StartedAt",
                table: "FipsSyncHistories",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FipsSyncHistories_Status",
                table: "FipsSyncHistories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FipsSyncHistories_SyncType",
                table: "FipsSyncHistories",
                column: "SyncType");

            migrationBuilder.CreateIndex(
                name: "IX_FipsSyncHistories_TargetEnvironment",
                table: "FipsSyncHistories",
                column: "TargetEnvironment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FipsSyncHistories");
        }
    }
}
