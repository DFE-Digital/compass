using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseScales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResponseScaleId",
                table: "SurveyQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResponseScales",
                columns: table => new
                {
                    ResponseScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    InputType = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseScales", x => x.ResponseScaleId);
                });

            migrationBuilder.CreateTable(
                name: "ResponseScaleOptions",
                columns: table => new
                {
                    ResponseScaleOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseScaleOptions", x => x.ResponseScaleOptionId);
                    table.ForeignKey(
                        name: "FK_ResponseScaleOptions_ResponseScales_ResponseScaleId",
                        column: x => x.ResponseScaleId,
                        principalTable: "ResponseScales",
                        principalColumn: "ResponseScaleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_ResponseScaleId",
                table: "SurveyQuestions",
                column: "ResponseScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseScaleOptions_ResponseScaleId",
                table: "ResponseScaleOptions",
                column: "ResponseScaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyQuestions_ResponseScales_ResponseScaleId",
                table: "SurveyQuestions",
                column: "ResponseScaleId",
                principalTable: "ResponseScales",
                principalColumn: "ResponseScaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyQuestions_ResponseScales_ResponseScaleId",
                table: "SurveyQuestions");

            migrationBuilder.DropTable(
                name: "ResponseScaleOptions");

            migrationBuilder.DropTable(
                name: "ResponseScales");

            migrationBuilder.DropIndex(
                name: "IX_SurveyQuestions_ResponseScaleId",
                table: "SurveyQuestions");

            migrationBuilder.DropColumn(
                name: "ResponseScaleId",
                table: "SurveyQuestions");
        }
    }
}
