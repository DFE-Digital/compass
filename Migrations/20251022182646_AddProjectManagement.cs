using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MissionId",
                table: "Objectives",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Milestones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FundingSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Missions_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Aim = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Outcomes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MissionId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsFlagship = table.Column<bool>(type: "bit", nullable: false),
                    RagStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RagJustification = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FundingSourceId = table.Column<int>(type: "int", nullable: true),
                    TotalPermFte = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    TotalMspFte = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_FundingSources_FundingSourceId",
                        column: x => x.FundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DependsOnProjectId = table.Column<int>(type: "int", nullable: false),
                    DependencyType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDependencies_Projects_DependsOnProjectId",
                        column: x => x.DependsOnProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDependencies_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProductFipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProductUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectProducts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRagHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    RagStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRagHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectRagHistories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSuccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    SuccessDescription = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RecordedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RecordedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSuccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectSuccesses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ProjectId",
                table: "Risks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_MissionId",
                table: "Objectives",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId",
                table: "Milestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ProjectId",
                table: "Issues",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ProjectId",
                table: "Actions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSources_Code",
                table: "FundingSources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundingSources_IsActive",
                table: "FundingSources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSources_SortOrder",
                table: "FundingSources",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_OwnerUserId",
                table: "Missions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_Status",
                table: "Missions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependencies_DependsOnProjectId",
                table: "ProjectDependencies",
                column: "DependsOnProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependencies_ProjectId",
                table: "ProjectDependencies",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProducts_ProductFipsId",
                table: "ProjectProducts",
                column: "ProductFipsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProducts_ProjectId",
                table: "ProjectProducts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRagHistories_ChangedAt",
                table: "ProjectRagHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRagHistories_ProjectId",
                table: "ProjectRagHistories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_FundingSourceId",
                table: "Projects",
                column: "FundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_IsFlagship",
                table: "Projects",
                column: "IsFlagship");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_MissionId",
                table: "Projects",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_RagStatus",
                table: "Projects",
                column: "RagStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_StartDate",
                table: "Projects",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TargetDeliveryDate",
                table: "Projects",
                column: "TargetDeliveryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSuccesses_ProjectId",
                table: "ProjectSuccesses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSuccesses_RecordedAt",
                table: "ProjectSuccesses",
                column: "RecordedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Projects_ProjectId",
                table: "Actions",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Projects_ProjectId",
                table: "Issues",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_Projects_ProjectId",
                table: "Milestones",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Objectives_Missions_MissionId",
                table: "Objectives",
                column: "MissionId",
                principalTable: "Missions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Projects_ProjectId",
                table: "Risks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Projects_ProjectId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Projects_ProjectId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_Projects_ProjectId",
                table: "Milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_Objectives_Missions_MissionId",
                table: "Objectives");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Projects_ProjectId",
                table: "Risks");

            migrationBuilder.DropTable(
                name: "ProjectDependencies");

            migrationBuilder.DropTable(
                name: "ProjectProducts");

            migrationBuilder.DropTable(
                name: "ProjectRagHistories");

            migrationBuilder.DropTable(
                name: "ProjectSuccesses");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "FundingSources");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropIndex(
                name: "IX_Risks_ProjectId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Objectives_MissionId",
                table: "Objectives");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_ProjectId",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ProjectId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ProjectId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "MissionId",
                table: "Objectives");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Actions");
        }
    }
}
