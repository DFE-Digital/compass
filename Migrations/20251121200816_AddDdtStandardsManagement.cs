using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDdtStandardsManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DdtStandardId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DdtStandards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegacyId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StandardUuid = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    HowToMeet = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Governance = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    GovernanceApproval = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PreviousVersion = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DraftCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstPublished = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LegalStandard = table.Column<bool>(type: "bit", nullable: false),
                    LegalBasis = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ValidityPeriod = table.Column<int>(type: "int", nullable: true),
                    RelatedGuidance = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    IsModified = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandards_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExternalCategoryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardCategories_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CommentType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardComments_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdtStandardComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardContacts_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdtStandardContacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardOwners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardOwners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardOwners_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdtStandardOwners_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardPhases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    PhaseLookupId = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardPhases_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdtStandardPhases_PhaseLookups_PhaseLookupId",
                        column: x => x.PhaseLookupId,
                        principalTable: "PhaseLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardSubCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExternalSubCategoryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardSubCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardSubCategories_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardValidationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    RuleId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ValidationType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Validator = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Config = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardValidationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardValidationRules_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdtStandardVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PreviousVersion = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VersionType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangeSummary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangeDetails = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    IsBreakingChange = table.Column<bool>(type: "bit", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    PublishedByUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardVersions_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdtStandardVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DdtStandardVersions_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DdtStandardId",
                table: "AuditLogs",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardCategories_DdtStandardId",
                table: "DdtStandardCategories",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardComments_DdtStandardId",
                table: "DdtStandardComments",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardComments_UserId",
                table: "DdtStandardComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardContacts_DdtStandardId",
                table: "DdtStandardContacts",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardContacts_UserId",
                table: "DdtStandardContacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardOwners_DdtStandardId",
                table: "DdtStandardOwners",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardOwners_UserId",
                table: "DdtStandardOwners",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardPhases_DdtStandardId",
                table: "DdtStandardPhases",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardPhases_PhaseLookupId",
                table: "DdtStandardPhases",
                column: "PhaseLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandards_CreatorUserId",
                table: "DdtStandards",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardSubCategories_DdtStandardId",
                table: "DdtStandardSubCategories",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardValidationRules_DdtStandardId",
                table: "DdtStandardValidationRules",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardVersions_CreatedByUserId",
                table: "DdtStandardVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardVersions_DdtStandardId",
                table: "DdtStandardVersions",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardVersions_PublishedByUserId",
                table: "DdtStandardVersions",
                column: "PublishedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_DdtStandards_DdtStandardId",
                table: "AuditLogs",
                column: "DdtStandardId",
                principalTable: "DdtStandards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_DdtStandards_DdtStandardId",
                table: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DdtStandardCategories");

            migrationBuilder.DropTable(
                name: "DdtStandardComments");

            migrationBuilder.DropTable(
                name: "DdtStandardContacts");

            migrationBuilder.DropTable(
                name: "DdtStandardOwners");

            migrationBuilder.DropTable(
                name: "DdtStandardPhases");

            migrationBuilder.DropTable(
                name: "DdtStandardSubCategories");

            migrationBuilder.DropTable(
                name: "DdtStandardValidationRules");

            migrationBuilder.DropTable(
                name: "DdtStandardVersions");

            migrationBuilder.DropTable(
                name: "DdtStandards");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_DdtStandardId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DdtStandardId",
                table: "AuditLogs");
        }
    }
}
