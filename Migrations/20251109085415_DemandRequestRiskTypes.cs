using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class DemandRequestRiskTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemandRequestRiskTypes",
                columns: table => new
                {
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    RiskTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestRiskTypes", x => new { x.DemandRequestId, x.RiskTypeId });
                    table.ForeignKey(
                        name: "FK_DemandRequestRiskTypes_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DemandRequestRiskTypes_RiskTypes_RiskTypeId",
                        column: x => x.RiskTypeId,
                        principalTable: "RiskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestRiskTypes_RiskTypeId",
                table: "DemandRequestRiskTypes",
                column: "RiskTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandRequestRiskTypes");
        }
    }
}
