using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDdatFrameworkModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DdatFrameworkVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionIdentifier = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    VersionName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SkillsCsvUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RolesCsvUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SkillsCsvPath = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RolesCsvPath = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    SkillsCount = table.Column<int>(type: "int", nullable: false),
                    RolesCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdatFrameworkVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DdatFrameworkChangeNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FrameworkVersionId = table.Column<int>(type: "int", nullable: false),
                    Page = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangeNote = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdatFrameworkChangeNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdatFrameworkChangeNotes_DdatFrameworkVersions_FrameworkVersionId",
                        column: x => x.FrameworkVersionId,
                        principalTable: "DdatFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdatFrameworkRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleFamily = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RoleDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    RoleLevel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RoleLevelDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    RoleType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FrameworkVersionId = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdatFrameworkRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdatFrameworkRoles_DdatFrameworkVersions_FrameworkVersionId",
                        column: x => x.FrameworkVersionId,
                        principalTable: "DdatFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DdatFrameworkSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SkillDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    AwarenessDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    WorkingDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    PractitionerDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ExpertDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    RolesThatRequireSkill = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    FrameworkVersionId = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdatFrameworkSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdatFrameworkSkills_DdatFrameworkVersions_FrameworkVersionId",
                        column: x => x.FrameworkVersionId,
                        principalTable: "DdatFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DdatFrameworkRoleSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdatFrameworkRoleId = table.Column<int>(type: "int", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SkillDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    SkillLevel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SkillLevelDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdatFrameworkRoleSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdatFrameworkRoleSkills_DdatFrameworkRoles_DdatFrameworkRoleId",
                        column: x => x.DdatFrameworkRoleId,
                        principalTable: "DdatFrameworkRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DdatFrameworkSkillGradeMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdatFrameworkSkillId = table.Column<int>(type: "int", nullable: false),
                    CapabilityLevel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdatFrameworkSkillGradeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdatFrameworkSkillGradeMappings_DdatFrameworkSkills_DdatFrameworkSkillId",
                        column: x => x.DdatFrameworkSkillId,
                        principalTable: "DdatFrameworkSkills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkChangeNotes_FrameworkVersionId",
                table: "DdatFrameworkChangeNotes",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkChangeNotes_Timestamp",
                table: "DdatFrameworkChangeNotes",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkRoles_FrameworkVersionId",
                table: "DdatFrameworkRoles",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkRoles_IsArchived",
                table: "DdatFrameworkRoles",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkRoles_Role_RoleLevel_FrameworkVersionId",
                table: "DdatFrameworkRoles",
                columns: new[] { "Role", "RoleLevel", "FrameworkVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkRoleSkills_DdatFrameworkRoleId_SkillName_SkillLevel",
                table: "DdatFrameworkRoleSkills",
                columns: new[] { "DdatFrameworkRoleId", "SkillName", "SkillLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkSkillGradeMappings_DdatFrameworkSkillId_CapabilityLevel_Grade",
                table: "DdatFrameworkSkillGradeMappings",
                columns: new[] { "DdatFrameworkSkillId", "CapabilityLevel", "Grade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkSkills_FrameworkVersionId",
                table: "DdatFrameworkSkills",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkSkills_IsArchived",
                table: "DdatFrameworkSkills",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkSkills_SkillName_FrameworkVersionId",
                table: "DdatFrameworkSkills",
                columns: new[] { "SkillName", "FrameworkVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkVersions_IsActive",
                table: "DdatFrameworkVersions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DdatFrameworkVersions_VersionIdentifier",
                table: "DdatFrameworkVersions",
                column: "VersionIdentifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DdatFrameworkChangeNotes");

            migrationBuilder.DropTable(
                name: "DdatFrameworkRoleSkills");

            migrationBuilder.DropTable(
                name: "DdatFrameworkSkillGradeMappings");

            migrationBuilder.DropTable(
                name: "DdatFrameworkRoles");

            migrationBuilder.DropTable(
                name: "DdatFrameworkSkills");

            migrationBuilder.DropTable(
                name: "DdatFrameworkVersions");
        }
    }
}
