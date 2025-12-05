using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDdatFrameworkRoleAndGradeToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DdatFrameworkRoleId",
                table: "UserProfessionalProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubstantiveGrade",
                table: "UserProfessionalProfiles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserDdatFrameworkSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    DdatFrameworkSkillId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDdatFrameworkSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDdatFrameworkSkills_DdatFrameworkSkills_DdatFrameworkSkillId",
                        column: x => x.DdatFrameworkSkillId,
                        principalTable: "DdatFrameworkSkills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDdatFrameworkSkills_UserProfessionalProfiles_UserProfessionalProfileId",
                        column: x => x.UserProfessionalProfileId,
                        principalTable: "UserProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfessionalProfiles_DdatFrameworkRoleId",
                table: "UserProfessionalProfiles",
                column: "DdatFrameworkRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDdatFrameworkSkills_DdatFrameworkSkillId",
                table: "UserDdatFrameworkSkills",
                column: "DdatFrameworkSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDdatFrameworkSkills_UserProfessionalProfileId_DdatFrameworkSkillId",
                table: "UserDdatFrameworkSkills",
                columns: new[] { "UserProfessionalProfileId", "DdatFrameworkSkillId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfessionalProfiles_DdatFrameworkRoles_DdatFrameworkRoleId",
                table: "UserProfessionalProfiles",
                column: "DdatFrameworkRoleId",
                principalTable: "DdatFrameworkRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProfessionalProfiles_DdatFrameworkRoles_DdatFrameworkRoleId",
                table: "UserProfessionalProfiles");

            migrationBuilder.DropTable(
                name: "UserDdatFrameworkSkills");

            migrationBuilder.DropIndex(
                name: "IX_UserProfessionalProfiles_DdatFrameworkRoleId",
                table: "UserProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "DdatFrameworkRoleId",
                table: "UserProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "SubstantiveGrade",
                table: "UserProfessionalProfiles");
        }
    }
}
