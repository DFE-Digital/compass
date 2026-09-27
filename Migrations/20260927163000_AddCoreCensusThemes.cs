using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260927163000_AddCoreCensusThemes")]
    public partial class AddCoreCensusThemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoreThemeKey",
                table: "ServiceDataModelGroups",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelGroups_ServiceDataModelVersionId_CoreThemeKey",
                table: "ServiceDataModelGroups",
                columns: new[] { "ServiceDataModelVersionId", "CoreThemeKey" });

            migrationBuilder.CreateTable(
                name: "CoreCensusThemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StableKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Guidance = table.Column<string>(type: "nvarchar(max)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsServiceOffering = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreCensusThemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoreCensusThemeFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoreCensusThemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StableKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Guidance = table.Column<string>(type: "nvarchar(max)", maxLength: 2000, nullable: true),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    SchemaSourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    CountsTowardsCompletion = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    VisibilityRuleJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreCensusThemeFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoreCensusThemeFields_CoreCensusThemes_CoreCensusThemeId",
                        column: x => x.CoreCensusThemeId,
                        principalTable: "CoreCensusThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoreCensusThemeFieldOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoreCensusThemeFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreCensusThemeFieldOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoreCensusThemeFieldOptions_CoreCensusThemeFields_CoreCensusThemeFieldId",
                        column: x => x.CoreCensusThemeFieldId,
                        principalTable: "CoreCensusThemeFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoreCensusThemes_StableKey",
                table: "CoreCensusThemes",
                column: "StableKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoreCensusThemeFields_CoreCensusThemeId_StableKey",
                table: "CoreCensusThemeFields",
                columns: new[] { "CoreCensusThemeId", "StableKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoreCensusThemeFieldOptions_CoreCensusThemeFieldId_ValueKey",
                table: "CoreCensusThemeFieldOptions",
                columns: new[] { "CoreCensusThemeFieldId", "ValueKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoreCensusThemeFieldOptions");

            migrationBuilder.DropTable(
                name: "CoreCensusThemeFields");

            migrationBuilder.DropTable(
                name: "CoreCensusThemes");

            migrationBuilder.DropIndex(
                name: "IX_ServiceDataModelGroups_ServiceDataModelVersionId_CoreThemeKey",
                table: "ServiceDataModelGroups");

            migrationBuilder.DropColumn(
                name: "CoreThemeKey",
                table: "ServiceDataModelGroups");
        }
    }
}
