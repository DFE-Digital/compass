using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignDecisionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent guard: some environments restored the database from a previous
            // pre-release where DDR tables already exist but the migration history row is
            // missing. Drop in dependency order so the CREATE statements below succeed
            // cleanly. Safe because this is the introducing migration for these tables.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ddr_alternative]', N'U') IS NOT NULL DROP TABLE [ddr_alternative];
IF OBJECT_ID(N'[ddr_audit_event]', N'U') IS NOT NULL DROP TABLE [ddr_audit_event];
IF OBJECT_ID(N'[ddr_comment]', N'U') IS NOT NULL DROP TABLE [ddr_comment];
IF OBJECT_ID(N'[ddr_component_pattern_link]', N'U') IS NOT NULL DROP TABLE [ddr_component_pattern_link];
IF OBJECT_ID(N'[ddr_evidence]', N'U') IS NOT NULL DROP TABLE [ddr_evidence];
IF OBJECT_ID(N'[ddr_github_issue_link]', N'U') IS NOT NULL DROP TABLE [ddr_github_issue_link];
IF OBJECT_ID(N'[ddr_insight_classification]', N'U') IS NOT NULL DROP TABLE [ddr_insight_classification];
IF OBJECT_ID(N'[ddr_recommended_follow_up]', N'U') IS NOT NULL DROP TABLE [ddr_recommended_follow_up];
IF OBJECT_ID(N'[ddr_record_product_link]', N'U') IS NOT NULL DROP TABLE [ddr_record_product_link];
IF OBJECT_ID(N'[ddr_record_work_item_link]', N'U') IS NOT NULL DROP TABLE [ddr_record_work_item_link];
IF OBJECT_ID(N'[ddr_related_record]', N'U') IS NOT NULL DROP TABLE [ddr_related_record];
IF OBJECT_ID(N'[ddr_standard_link]', N'U') IS NOT NULL DROP TABLE [ddr_standard_link];
IF OBJECT_ID(N'[ddr_record]', N'U') IS NOT NULL DROP TABLE [ddr_record];
IF OBJECT_ID(N'[ddr_feature_setting]', N'U') IS NOT NULL DROP TABLE [ddr_feature_setting];
");

            migrationBuilder.CreateTable(
                name: "ddr_feature_setting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_feature_setting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ddr_record",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ShortTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ContextProblemStatement = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    ConsequencesTradeoffs = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    DeviationFlag = table.Column<bool>(type: "bit", nullable: false),
                    DeviationType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DeviationDetails = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ApprovalRoute = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReviewTrigger = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetrospectiveRecord = table.Column<bool>(type: "bit", nullable: false),
                    OriginalDecisionDate = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RetrospectiveContext = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CurrentValidity = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CurrentValidityRationale = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    MessageToDesignOps = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_record", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ddr_alternative",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    AlternativeText = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_alternative", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_alternative_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_audit_event",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PreviousValue = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_audit_event", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_audit_event_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_comment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    CommentType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_comment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_comment_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_component_pattern_link",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ItemType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ItemUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_component_pattern_link", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_component_pattern_link_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_evidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    EvidenceTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EvidenceUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EvidenceSummary = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_evidence_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_github_issue_link",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    IssueUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BacklogType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IssueTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_github_issue_link", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_github_issue_link_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_insight_classification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_insight_classification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_insight_classification_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_recommended_follow_up",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    FollowUpAction = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TargetBacklog = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_recommended_follow_up", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_recommended_follow_up_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_record_product_link",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    FipsProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_record_product_link", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_record_product_link_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_record_work_item_link",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    WorkItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_record_work_item_link", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_record_work_item_link_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_related_record",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    RelatedDesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_related_record", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_related_record_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ddr_standard_link",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignDecisionRecordId = table.Column<int>(type: "int", nullable: false),
                    StandardType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StandardReference = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StandardTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StandardUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ddr_standard_link", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ddr_standard_link_ddr_record_DesignDecisionRecordId",
                        column: x => x.DesignDecisionRecordId,
                        principalTable: "ddr_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ddr_alternative_DesignDecisionRecordId",
                table: "ddr_alternative",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_audit_event_CreatedAt",
                table: "ddr_audit_event",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_audit_event_DesignDecisionRecordId",
                table: "ddr_audit_event",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_comment_CreatedAt",
                table: "ddr_comment",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_comment_DesignDecisionRecordId",
                table: "ddr_comment",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_component_pattern_link_DesignDecisionRecordId",
                table: "ddr_component_pattern_link",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_evidence_DesignDecisionRecordId",
                table: "ddr_evidence",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_feature_setting_SettingKey",
                table: "ddr_feature_setting",
                column: "SettingKey");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_feature_setting_UpdatedAt",
                table: "ddr_feature_setting",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_github_issue_link_DesignDecisionRecordId",
                table: "ddr_github_issue_link",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_insight_classification_Classification",
                table: "ddr_insight_classification",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_insight_classification_DesignDecisionRecordId",
                table: "ddr_insight_classification",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_recommended_follow_up_DesignDecisionRecordId",
                table: "ddr_recommended_follow_up",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_recommended_follow_up_Status",
                table: "ddr_recommended_follow_up",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_Category",
                table: "ddr_record",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_CreatedAt",
                table: "ddr_record",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_DeviationFlag",
                table: "ddr_record",
                column: "DeviationFlag");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_Reference",
                table: "ddr_record",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_ReviewDate",
                table: "ddr_record",
                column: "ReviewDate");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_Status",
                table: "ddr_record",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_product_link_DesignDecisionRecordId_FipsProductId",
                table: "ddr_record_product_link",
                columns: new[] { "DesignDecisionRecordId", "FipsProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_product_link_FipsProductId",
                table: "ddr_record_product_link",
                column: "FipsProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_work_item_link_DesignDecisionRecordId_WorkItemId",
                table: "ddr_record_work_item_link",
                columns: new[] { "DesignDecisionRecordId", "WorkItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ddr_record_work_item_link_WorkItemId",
                table: "ddr_record_work_item_link",
                column: "WorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_related_record_DesignDecisionRecordId",
                table: "ddr_related_record",
                column: "DesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_related_record_RelatedDesignDecisionRecordId",
                table: "ddr_related_record",
                column: "RelatedDesignDecisionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ddr_standard_link_DesignDecisionRecordId",
                table: "ddr_standard_link",
                column: "DesignDecisionRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ddr_alternative");

            migrationBuilder.DropTable(
                name: "ddr_audit_event");

            migrationBuilder.DropTable(
                name: "ddr_comment");

            migrationBuilder.DropTable(
                name: "ddr_component_pattern_link");

            migrationBuilder.DropTable(
                name: "ddr_evidence");

            migrationBuilder.DropTable(
                name: "ddr_feature_setting");

            migrationBuilder.DropTable(
                name: "ddr_github_issue_link");

            migrationBuilder.DropTable(
                name: "ddr_insight_classification");

            migrationBuilder.DropTable(
                name: "ddr_recommended_follow_up");

            migrationBuilder.DropTable(
                name: "ddr_record_product_link");

            migrationBuilder.DropTable(
                name: "ddr_record_work_item_link");

            migrationBuilder.DropTable(
                name: "ddr_related_record");

            migrationBuilder.DropTable(
                name: "ddr_standard_link");

            migrationBuilder.DropTable(
                name: "ddr_record");
        }
    }
}
