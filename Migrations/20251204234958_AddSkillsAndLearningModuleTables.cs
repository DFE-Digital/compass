using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillsAndLearningModuleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HOPS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Profession = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOPS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HOPS_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingCourses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Format = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Mode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProfessionTags = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CapabilityTags = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingCourses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfessionalProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Profession = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Skills = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CapabilityGaps = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    HeadOfProfessionId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfessionalProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfessionalProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DateRequested = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateApproved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAttended = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutcomeRating = table.Column<int>(type: "int", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CostActual = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EvidenceFileUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRecords_TrainingCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "TrainingCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    CustomCourseTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Justification = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ProfessionAlignment = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: true),
                    ApproverComments = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRequests_Decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "Decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingRequests_TrainingCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "TrainingCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HOPS_Profession",
                table: "HOPS",
                column: "Profession");

            migrationBuilder.CreateIndex(
                name: "IX_HOPS_UserId",
                table: "HOPS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HOPS_UserId_Profession",
                table: "HOPS",
                columns: new[] { "UserId", "Profession" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCourses_Active",
                table: "TrainingCourses",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCourses_Provider",
                table: "TrainingCourses",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCourses_Title",
                table: "TrainingCourses",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_CourseId",
                table: "TrainingRecords",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_DateAttended",
                table: "TrainingRecords",
                column: "DateAttended");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_Status",
                table: "TrainingRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_UserId",
                table: "TrainingRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_CourseId",
                table: "TrainingRequests",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_CreatedAt",
                table: "TrainingRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_DecisionId",
                table: "TrainingRequests",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_Status",
                table: "TrainingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_UserId",
                table: "TrainingRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfessionalProfiles_Profession",
                table: "UserProfessionalProfiles",
                column: "Profession");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfessionalProfiles_UserId",
                table: "UserProfessionalProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HOPS");

            migrationBuilder.DropTable(
                name: "TrainingRecords");

            migrationBuilder.DropTable(
                name: "TrainingRequests");

            migrationBuilder.DropTable(
                name: "UserProfessionalProfiles");

            migrationBuilder.DropTable(
                name: "TrainingCourses");
        }
    }
}
