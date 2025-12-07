using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionAndSkillsManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HOPS_Profession",
                table: "HOPS");

            migrationBuilder.DropIndex(
                name: "IX_HOPS_UserId_Profession",
                table: "HOPS");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "UserProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "Profession",
                table: "HOPS");

            migrationBuilder.AddColumn<int>(
                name: "DdatProfessionId",
                table: "UserProfessionalProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DdatProfessionId",
                table: "HOPS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LearningBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialYear = table.Column<int>(type: "int", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Spent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Forecasted = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdatProfessionId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessionSkills_DdatProfessions_DdatProfessionId",
                        column: x => x.DdatProfessionId,
                        principalTable: "DdatProfessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfessionSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingNudges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CapabilityGap = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingNudges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingNudges_TrainingCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "TrainingCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingNudges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfessionalProfileSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfessionalProfileSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfessionalProfileSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserProfessionalProfileSkills_UserProfessionalProfiles_UserProfessionalProfileId",
                        column: x => x.UserProfessionalProfileId,
                        principalTable: "UserProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfessionalProfiles_DdatProfessionId",
                table: "UserProfessionalProfiles",
                column: "DdatProfessionId");

            migrationBuilder.CreateIndex(
                name: "IX_HOPS_DdatProfessionId",
                table: "HOPS",
                column: "DdatProfessionId");

            migrationBuilder.CreateIndex(
                name: "IX_HOPS_UserId_DdatProfessionId",
                table: "HOPS",
                columns: new[] { "UserId", "DdatProfessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningBudgets_FinancialYear",
                table: "LearningBudgets",
                column: "FinancialYear");

            migrationBuilder.CreateIndex(
                name: "IX_LearningBudgets_FinancialYear_IsActive",
                table: "LearningBudgets",
                columns: new[] { "FinancialYear", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionSkills_DdatProfessionId",
                table: "ProfessionSkills",
                column: "DdatProfessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionSkills_DdatProfessionId_SkillId",
                table: "ProfessionSkills",
                columns: new[] { "DdatProfessionId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionSkills_SkillId",
                table: "ProfessionSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingNudges_CourseId",
                table: "TrainingNudges",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingNudges_UserId",
                table: "TrainingNudges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingNudges_UserId_IsActive",
                table: "TrainingNudges",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfessionalProfileSkills_SkillId",
                table: "UserProfessionalProfileSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfessionalProfileSkills_UserProfessionalProfileId_SkillId",
                table: "UserProfessionalProfileSkills",
                columns: new[] { "UserProfessionalProfileId", "SkillId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HOPS_DdatProfessions_DdatProfessionId",
                table: "HOPS",
                column: "DdatProfessionId",
                principalTable: "DdatProfessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfessionalProfiles_DdatProfessions_DdatProfessionId",
                table: "UserProfessionalProfiles",
                column: "DdatProfessionId",
                principalTable: "DdatProfessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HOPS_DdatProfessions_DdatProfessionId",
                table: "HOPS");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfessionalProfiles_DdatProfessions_DdatProfessionId",
                table: "UserProfessionalProfiles");

            migrationBuilder.DropTable(
                name: "LearningBudgets");

            migrationBuilder.DropTable(
                name: "ProfessionSkills");

            migrationBuilder.DropTable(
                name: "TrainingNudges");

            migrationBuilder.DropTable(
                name: "UserProfessionalProfileSkills");

            migrationBuilder.DropIndex(
                name: "IX_UserProfessionalProfiles_DdatProfessionId",
                table: "UserProfessionalProfiles");

            migrationBuilder.DropIndex(
                name: "IX_HOPS_DdatProfessionId",
                table: "HOPS");

            migrationBuilder.DropIndex(
                name: "IX_HOPS_UserId_DdatProfessionId",
                table: "HOPS");

            migrationBuilder.DropColumn(
                name: "DdatProfessionId",
                table: "UserProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "DdatProfessionId",
                table: "HOPS");

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "UserProfessionalProfiles",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Profession",
                table: "HOPS",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_HOPS_Profession",
                table: "HOPS",
                column: "Profession");

            migrationBuilder.CreateIndex(
                name: "IX_HOPS_UserId_Profession",
                table: "HOPS",
                columns: new[] { "UserId", "Profession" },
                unique: true);
        }
    }
}
