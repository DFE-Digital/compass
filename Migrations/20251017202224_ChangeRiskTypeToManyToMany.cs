using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRiskTypeToManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskTypes_RiskTypeId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskTypeId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskTypeId",
                table: "Risks");

            migrationBuilder.CreateTable(
                name: "RiskRiskTypes",
                columns: table => new
                {
                    RiskId = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskRiskTypes", x => new { x.RiskId, x.RiskTypeId });
                    table.ForeignKey(
                        name: "FK_RiskRiskTypes_RiskTypes_RiskTypeId",
                        column: x => x.RiskTypeId,
                        principalTable: "RiskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskRiskTypes_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskRiskTypes_RiskTypeId",
                table: "RiskRiskTypes",
                column: "RiskTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskRiskTypes");

            migrationBuilder.AddColumn<int>(
                name: "RiskTypeId",
                table: "Risks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskTypeId",
                table: "Risks",
                column: "RiskTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskTypes_RiskTypeId",
                table: "Risks",
                column: "RiskTypeId",
                principalTable: "RiskTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
