using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLineBusinessAreas",
                columns: table => new
                {
                    ServiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLineBusinessAreas", x => new { x.ServiceLineId, x.BusinessAreaLookupId });
                    table.ForeignKey(
                        name: "FK_ServiceLineBusinessAreas_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceLineBusinessAreas_ServiceLines_ServiceLineId",
                        column: x => x.ServiceLineId,
                        principalTable: "ServiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLineDivisions",
                columns: table => new
                {
                    ServiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DivisionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLineDivisions", x => new { x.ServiceLineId, x.DivisionId });
                    table.ForeignKey(
                        name: "FK_ServiceLineDivisions_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceLineDivisions_ServiceLines_ServiceLineId",
                        column: x => x.ServiceLineId,
                        principalTable: "ServiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLineProducts",
                columns: table => new
                {
                    ServiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLineProducts", x => new { x.ServiceLineId, x.CMDBProductId });
                    table.ForeignKey(
                        name: "FK_ServiceLineProducts_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceLineProducts_ServiceLines_ServiceLineId",
                        column: x => x.ServiceLineId,
                        principalTable: "ServiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLineProjects",
                columns: table => new
                {
                    ServiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLineProjects", x => new { x.ServiceLineId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_ServiceLineProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceLineProjects_ServiceLines_ServiceLineId",
                        column: x => x.ServiceLineId,
                        principalTable: "ServiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLineBusinessAreas_BusinessAreaLookupId",
                table: "ServiceLineBusinessAreas",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLineDivisions_DivisionId",
                table: "ServiceLineDivisions",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLineProducts_CMDBProductId",
                table: "ServiceLineProducts",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLineProjects_ProjectId",
                table: "ServiceLineProjects",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceLineBusinessAreas");

            migrationBuilder.DropTable(
                name: "ServiceLineDivisions");

            migrationBuilder.DropTable(
                name: "ServiceLineProducts");

            migrationBuilder.DropTable(
                name: "ServiceLineProjects");

            migrationBuilder.DropTable(
                name: "ServiceLines");
        }
    }
}
