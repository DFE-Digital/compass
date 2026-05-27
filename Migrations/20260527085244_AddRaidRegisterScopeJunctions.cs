using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidRegisterScopeJunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaidRegisterBusinessAreas",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterBusinessAreas", x => new { x.RaidRegisterId, x.BusinessAreaLookupId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterBusinessAreas_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisterBusinessAreas_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterDirectorates",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    DirectorateLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterDirectorates", x => new { x.RaidRegisterId, x.DirectorateLookupId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterDirectorates_DirectorateLookups_DirectorateLookupId",
                        column: x => x.DirectorateLookupId,
                        principalTable: "DirectorateLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisterDirectorates_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterBusinessAreas_BusinessAreaLookupId",
                table: "RaidRegisterBusinessAreas",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterDirectorates_DirectorateLookupId",
                table: "RaidRegisterDirectorates",
                column: "DirectorateLookupId");

            migrationBuilder.Sql(
                """
                INSERT INTO RaidRegisterDirectorates (RaidRegisterId, DirectorateLookupId)
                SELECT Id, DirectorateLookupId
                FROM RaidRegisters
                WHERE DirectorateLookupId IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM RaidRegisterDirectorates d
                    WHERE d.RaidRegisterId = RaidRegisters.Id
                      AND d.DirectorateLookupId = RaidRegisters.DirectorateLookupId);
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO RaidRegisterBusinessAreas (RaidRegisterId, BusinessAreaLookupId)
                SELECT Id, BusinessAreaLookupId
                FROM RaidRegisters
                WHERE BusinessAreaLookupId IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM RaidRegisterBusinessAreas b
                    WHERE b.RaidRegisterId = RaidRegisters.Id
                      AND b.BusinessAreaLookupId = RaidRegisters.BusinessAreaLookupId);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidRegisterBusinessAreas");

            migrationBuilder.DropTable(
                name: "RaidRegisterDirectorates");
        }
    }
}
