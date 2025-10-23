using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDraftModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Aim = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsFlagship = table.Column<bool>(type: "bit", nullable: false),
                    RagStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RagJustification = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TotalPermFte = table.Column<decimal>(type: "decimal(5,2)", precision: 10, scale: 2, nullable: true),
                    TotalMspFte = table.Column<decimal>(type: "decimal(5,2)", precision: 10, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MissionIdsJson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FundingAllocationsJson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OutcomesJson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDrafts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDrafts_CreatedAt",
                table: "ProjectDrafts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDrafts_IsConfirmed",
                table: "ProjectDrafts",
                column: "IsConfirmed");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDrafts_SessionId",
                table: "ProjectDrafts",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDrafts_UserEmail",
                table: "ProjectDrafts",
                column: "UserEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDrafts");
        }
    }
}
