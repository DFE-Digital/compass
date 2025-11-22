using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddStandardCategoriesAndSubCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubCategoryName",
                table: "DdtStandardSubCategories");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "DdtStandardCategories");

            migrationBuilder.AddColumn<int>(
                name: "SubCategoryId",
                table: "DdtStandardSubCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "DdtStandardCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StandardCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandardSubCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardSubCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandardSubCategories_StandardCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "StandardCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardSubCategories_SubCategoryId",
                table: "DdtStandardSubCategories",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardCategories_CategoryId",
                table: "DdtStandardCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StandardSubCategories_CategoryId",
                table: "StandardSubCategories",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_DdtStandardCategories_StandardCategories_CategoryId",
                table: "DdtStandardCategories",
                column: "CategoryId",
                principalTable: "StandardCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DdtStandardSubCategories_StandardSubCategories_SubCategoryId",
                table: "DdtStandardSubCategories",
                column: "SubCategoryId",
                principalTable: "StandardSubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DdtStandardCategories_StandardCategories_CategoryId",
                table: "DdtStandardCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_DdtStandardSubCategories_StandardSubCategories_SubCategoryId",
                table: "DdtStandardSubCategories");

            migrationBuilder.DropTable(
                name: "StandardSubCategories");

            migrationBuilder.DropTable(
                name: "StandardCategories");

            migrationBuilder.DropIndex(
                name: "IX_DdtStandardSubCategories_SubCategoryId",
                table: "DdtStandardSubCategories");

            migrationBuilder.DropIndex(
                name: "IX_DdtStandardCategories_CategoryId",
                table: "DdtStandardCategories");

            migrationBuilder.DropColumn(
                name: "SubCategoryId",
                table: "DdtStandardSubCategories");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "DdtStandardCategories");

            migrationBuilder.AddColumn<string>(
                name: "SubCategoryName",
                table: "DdtStandardSubCategories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "DdtStandardCategories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }
    }
}
