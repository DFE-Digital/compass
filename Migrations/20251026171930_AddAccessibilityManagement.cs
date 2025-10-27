using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessibilityManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductAccessibilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProductPhase = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SlaResponseDays = table.Column<int>(type: "int", nullable: false),
                    ComplaintsEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrolledBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAccessibilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessibilityIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAccessibilityId = table.Column<int>(type: "int", nullable: false),
                    WcagCriteria = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    WcagLevel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    WcagVersion = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IdentifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdentifiedVia = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IssueDescription = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsResolving = table.Column<bool>(type: "bit", nullable: false),
                    PlannedResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NonResolutionReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActualResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessibilityIssues_ProductAccessibilities_ProductAccessibilityId",
                        column: x => x.ProductAccessibilityId,
                        principalTable: "ProductAccessibilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAccessibilityId = table.Column<int>(type: "int", nullable: false),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AuditType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReportUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditHistories_ProductAccessibilities_ProductAccessibilityId",
                        column: x => x.ProductAccessibilityId,
                        principalTable: "ProductAccessibilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAccessibilityId = table.Column<int>(type: "int", nullable: false),
                    ContactType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ContactDetail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactMethods_ProductAccessibilities_ProductAccessibilityId",
                        column: x => x.ProductAccessibilityId,
                        principalTable: "ProductAccessibilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessibilityIssueId = table.Column<int>(type: "int", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueComments_AccessibilityIssues_AccessibilityIssueId",
                        column: x => x.AccessibilityIssueId,
                        principalTable: "AccessibilityIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessibilityIssueId = table.Column<int>(type: "int", nullable: false),
                    FieldChanged = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangeNote = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueHistories_AccessibilityIssues_AccessibilityIssueId",
                        column: x => x.AccessibilityIssueId,
                        principalTable: "AccessibilityIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityIssues_ProductAccessibilityId",
                table: "AccessibilityIssues",
                column: "ProductAccessibilityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistories_ProductAccessibilityId",
                table: "AuditHistories",
                column: "ProductAccessibilityId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMethods_ProductAccessibilityId",
                table: "ContactMethods",
                column: "ProductAccessibilityId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueComments_AccessibilityIssueId",
                table: "IssueComments",
                column: "AccessibilityIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueHistories_AccessibilityIssueId",
                table: "IssueHistories",
                column: "AccessibilityIssueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditHistories");

            migrationBuilder.DropTable(
                name: "ContactMethods");

            migrationBuilder.DropTable(
                name: "IssueComments");

            migrationBuilder.DropTable(
                name: "IssueHistories");

            migrationBuilder.DropTable(
                name: "AccessibilityIssues");

            migrationBuilder.DropTable(
                name: "ProductAccessibilities");
        }
    }
}
