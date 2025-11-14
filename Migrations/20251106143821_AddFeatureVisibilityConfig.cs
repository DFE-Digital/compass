using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureVisibilityConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureVisibilityConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    IsGloballyVisible = table.Column<bool>(type: "bit", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    AllowedBusinessAreas = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequirePermissionForVisibility = table.Column<bool>(type: "bit", nullable: false),
                    VisibleFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VisibleUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureVisibilityConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureVisibilityConfigs_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureVisibilityConfigs_FeatureId",
                table: "FeatureVisibilityConfigs",
                column: "FeatureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureVisibilityConfigs_IsGloballyVisible",
                table: "FeatureVisibilityConfigs",
                column: "IsGloballyVisible");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureVisibilityConfigs_Scope",
                table: "FeatureVisibilityConfigs",
                column: "Scope");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureVisibilityConfigs");
        }
    }
}
