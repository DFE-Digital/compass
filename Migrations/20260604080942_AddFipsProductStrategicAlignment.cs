using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFipsProductStrategicAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMultiDepartmentProduct",
                table: "CMDBProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubjectToSpendControl",
                table: "CMDBProducts",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherDepartments",
                table: "CMDBProducts",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskAppetiteLookupId",
                table: "CMDBProducts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CMDBProductMissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductMissions_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductMissions_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductObjectives_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductObjectives_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductWorkItemTags",
                columns: table => new
                {
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkItemTagLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductWorkItemTags", x => new { x.CMDBProductId, x.WorkItemTagLookupId });
                    table.ForeignKey(
                        name: "FK_CMDBProductWorkItemTags_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductWorkItemTags_WorkItemTagLookups_WorkItemTagLookupId",
                        column: x => x.WorkItemTagLookupId,
                        principalTable: "WorkItemTagLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProducts_RiskAppetiteLookupId",
                table: "CMDBProducts",
                column: "RiskAppetiteLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductMissions_CMDBProductId_MissionId",
                table: "CMDBProductMissions",
                columns: new[] { "CMDBProductId", "MissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductMissions_MissionId",
                table: "CMDBProductMissions",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductObjectives_CMDBProductId_ObjectiveId",
                table: "CMDBProductObjectives",
                columns: new[] { "CMDBProductId", "ObjectiveId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductObjectives_ObjectiveId",
                table: "CMDBProductObjectives",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductWorkItemTags_WorkItemTagLookupId",
                table: "CMDBProductWorkItemTags",
                column: "WorkItemTagLookupId");

            migrationBuilder.AddForeignKey(
                name: "FK_CMDBProducts_RiskAppetiteLookups_RiskAppetiteLookupId",
                table: "CMDBProducts",
                column: "RiskAppetiteLookupId",
                principalTable: "RiskAppetiteLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CMDBProducts_RiskAppetiteLookups_RiskAppetiteLookupId",
                table: "CMDBProducts");

            migrationBuilder.DropTable(
                name: "CMDBProductMissions");

            migrationBuilder.DropTable(
                name: "CMDBProductObjectives");

            migrationBuilder.DropTable(
                name: "CMDBProductWorkItemTags");

            migrationBuilder.DropIndex(
                name: "IX_CMDBProducts_RiskAppetiteLookupId",
                table: "CMDBProducts");

            migrationBuilder.DropColumn(
                name: "IsMultiDepartmentProduct",
                table: "CMDBProducts");

            migrationBuilder.DropColumn(
                name: "IsSubjectToSpendControl",
                table: "CMDBProducts");

            migrationBuilder.DropColumn(
                name: "OtherDepartments",
                table: "CMDBProducts");

            migrationBuilder.DropColumn(
                name: "RiskAppetiteLookupId",
                table: "CMDBProducts");
        }
    }
}
