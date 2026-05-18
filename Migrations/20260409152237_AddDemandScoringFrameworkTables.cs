using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandScoringFrameworkTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScoringAnswersJson",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DemandScoringBandDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MinScaledInclusive = table.Column<int>(type: "int", nullable: false),
                    MaxScaledInclusive = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandScoringBandDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandScoringFrameworkSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    MaxPoints = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LegacyColumn = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandScoringFrameworkSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandScoringFrameworkQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    QuestionType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsScored = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    NumberMin = table.Column<int>(type: "int", nullable: true),
                    NumberMax = table.Column<int>(type: "int", nullable: true),
                    ContextKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandScoringFrameworkQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandScoringFrameworkQuestions_DemandScoringFrameworkSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "DemandScoringFrameworkSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandScoringFrameworkOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandScoringFrameworkOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandScoringFrameworkOptions_DemandScoringFrameworkQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "DemandScoringFrameworkQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandScoringBandDefinitions_SortOrder",
                table: "DemandScoringBandDefinitions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_DemandScoringFrameworkOptions_QuestionId_SortOrder",
                table: "DemandScoringFrameworkOptions",
                columns: new[] { "QuestionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandScoringFrameworkQuestions_Code",
                table: "DemandScoringFrameworkQuestions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandScoringFrameworkQuestions_SectionId",
                table: "DemandScoringFrameworkQuestions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandScoringFrameworkSections_Key",
                table: "DemandScoringFrameworkSections",
                column: "Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandScoringBandDefinitions");

            migrationBuilder.DropTable(
                name: "DemandScoringFrameworkOptions");

            migrationBuilder.DropTable(
                name: "DemandScoringFrameworkQuestions");

            migrationBuilder.DropTable(
                name: "DemandScoringFrameworkSections");

            migrationBuilder.DropColumn(
                name: "ScoringAnswersJson",
                table: "DemandPipelineRequests");
        }
    }
}
