using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadedCommentsToDdtStandardComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "DdtStandardComments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentCommentId",
                table: "DdtStandardComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "DdtStandardComments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolvedByUserId",
                table: "DdtStandardComments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StandardProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DfeFipsProductId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DfeProductName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandardProducts_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StandardProducts_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    StandardProductId = table.Column<int>(type: "int", nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardProducts_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdtStandardProducts_StandardProducts_StandardProductId",
                        column: x => x.StandardProductId,
                        principalTable: "StandardProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardComments_ParentCommentId",
                table: "DdtStandardComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardComments_ResolvedByUserId",
                table: "DdtStandardComments",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardProducts_DdtStandardId",
                table: "DdtStandardProducts",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardProducts_StandardProductId",
                table: "DdtStandardProducts",
                column: "StandardProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StandardProducts_CreatedByUserId",
                table: "StandardProducts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StandardProducts_ReviewedByUserId",
                table: "StandardProducts",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DdtStandardComments_DdtStandardComments_ParentCommentId",
                table: "DdtStandardComments",
                column: "ParentCommentId",
                principalTable: "DdtStandardComments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DdtStandardComments_Users_ResolvedByUserId",
                table: "DdtStandardComments",
                column: "ResolvedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DdtStandardComments_DdtStandardComments_ParentCommentId",
                table: "DdtStandardComments");

            migrationBuilder.DropForeignKey(
                name: "FK_DdtStandardComments_Users_ResolvedByUserId",
                table: "DdtStandardComments");

            migrationBuilder.DropTable(
                name: "DdtStandardProducts");

            migrationBuilder.DropTable(
                name: "StandardProducts");

            migrationBuilder.DropIndex(
                name: "IX_DdtStandardComments_ParentCommentId",
                table: "DdtStandardComments");

            migrationBuilder.DropIndex(
                name: "IX_DdtStandardComments_ResolvedByUserId",
                table: "DdtStandardComments");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "DdtStandardComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "DdtStandardComments");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "DdtStandardComments");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "DdtStandardComments");
        }
    }
}
