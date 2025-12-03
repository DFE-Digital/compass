using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementVerificationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatementVerificationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAccessibilityId = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestorEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestNotes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: true),
                    VerificationResult = table.Column<bool>(type: "bit", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailSentToRequestor = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentToAdmin = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementVerificationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatementVerificationRequests_ProductAccessibilities_ProductAccessibilityId",
                        column: x => x.ProductAccessibilityId,
                        principalTable: "ProductAccessibilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatementVerificationRequests_ProductAccessibilityId",
                table: "StatementVerificationRequests",
                column: "ProductAccessibilityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatementVerificationRequests");
        }
    }
}
