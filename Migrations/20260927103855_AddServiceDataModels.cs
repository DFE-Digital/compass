using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDataModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceDataModelAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OccurredUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StableKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    OwnerDisplayName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsRepeatable = table.Column<bool>(type: "bit", nullable: false),
                    IsReportable = table.Column<bool>(type: "bit", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "int", nullable: false),
                    ApplicabilityMode = table.Column<int>(type: "int", nullable: false),
                    RequiresReviewerAttestation = table.Column<bool>(type: "bit", nullable: false),
                    ProgressLabelThresholdPercent = table.Column<int>(type: "int", nullable: true),
                    DefaultDueDaysAfterPublish = table.Column<int>(type: "int", nullable: true),
                    ReviewCadenceLabel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelApplicabilityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductStatus = table.Column<int>(type: "int", nullable: true),
                    PhaseId = table.Column<int>(type: "int", nullable: true),
                    FipsTypeId = table.Column<int>(type: "int", nullable: true),
                    FipsBusinessAreaId = table.Column<int>(type: "int", nullable: true),
                    FipsDirectorateId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelApplicabilityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelApplicabilityRules_ServiceDataModels_ServiceDataModelId",
                        column: x => x.ServiceDataModelId,
                        principalTable: "ServiceDataModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelExplicitServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelExplicitServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelExplicitServices_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelExplicitServices_ServiceDataModels_ServiceDataModelId",
                        column: x => x.ServiceDataModelId,
                        principalTable: "ServiceDataModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PeriodLabel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RetiredUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DefinitionSnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelVersions_ServiceDataModels_ServiceDataModelId",
                        column: x => x.ServiceDataModelId,
                        principalTable: "ServiceDataModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAnsweredUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAnsweredByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewerAttestationNote = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ChangesRequestedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChangesRequestedNote = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    FieldCompletionPercent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    MandatoryCompletionPercent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    CurrentRevisionNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelAssignments_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelAssignments_ServiceDataModelVersions_ServiceDataModelVersionId",
                        column: x => x.ServiceDataModelVersionId,
                        principalTable: "ServiceDataModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StableKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Guidance = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelGroups_ServiceDataModelVersions_ServiceDataModelVersionId",
                        column: x => x.ServiceDataModelVersionId,
                        principalTable: "ServiceDataModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsSubmittedSnapshot = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelSubmissions_ServiceDataModelAssignments_ServiceDataModelAssignmentId",
                        column: x => x.ServiceDataModelAssignmentId,
                        principalTable: "ServiceDataModelAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StableKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Guidance = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    CountsTowardsCompletion = table.Column<bool>(type: "bit", nullable: false),
                    IsReportable = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    VisibilityRuleJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CanonicalAttributeKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ValidationPattern = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MinNumber = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxNumber = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelFields_ServiceDataModelGroups_ServiceDataModelGroupId",
                        column: x => x.ServiceDataModelGroupId,
                        principalTable: "ServiceDataModelGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    ValidationMessage = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelAnswers_ServiceDataModelFields_ServiceDataModelFieldId",
                        column: x => x.ServiceDataModelFieldId,
                        principalTable: "ServiceDataModelFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelAnswers_ServiceDataModelSubmissions_ServiceDataModelSubmissionId",
                        column: x => x.ServiceDataModelSubmissionId,
                        principalTable: "ServiceDataModelSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelFieldOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelFieldOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelFieldOptions_ServiceDataModelFields_ServiceDataModelFieldId",
                        column: x => x.ServiceDataModelFieldId,
                        principalTable: "ServiceDataModelFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDataModelProposedRegisterChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDataModelFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanonicalAttributeKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CurrentRegisterValue = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ProposedValue = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDataModelProposedRegisterChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelProposedRegisterChanges_ServiceDataModelAssignments_ServiceDataModelAssignmentId",
                        column: x => x.ServiceDataModelAssignmentId,
                        principalTable: "ServiceDataModelAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceDataModelProposedRegisterChanges_ServiceDataModelFields_ServiceDataModelFieldId",
                        column: x => x.ServiceDataModelFieldId,
                        principalTable: "ServiceDataModelFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAnswers_ServiceDataModelFieldId",
                table: "ServiceDataModelAnswers",
                column: "ServiceDataModelFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAnswers_ServiceDataModelSubmissionId_ServiceDataModelFieldId",
                table: "ServiceDataModelAnswers",
                columns: new[] { "ServiceDataModelSubmissionId", "ServiceDataModelFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelApplicabilityRules_ServiceDataModelId",
                table: "ServiceDataModelApplicabilityRules",
                column: "ServiceDataModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAssignments_CMDBProductId",
                table: "ServiceDataModelAssignments",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAssignments_DueUtc",
                table: "ServiceDataModelAssignments",
                column: "DueUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAssignments_ServiceDataModelVersionId_CMDBProductId_PeriodLabel",
                table: "ServiceDataModelAssignments",
                columns: new[] { "ServiceDataModelVersionId", "CMDBProductId", "PeriodLabel" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAssignments_Status",
                table: "ServiceDataModelAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAuditEvents_EntityType_EntityId",
                table: "ServiceDataModelAuditEvents",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelAuditEvents_OccurredUtc",
                table: "ServiceDataModelAuditEvents",
                column: "OccurredUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelExplicitServices_CMDBProductId",
                table: "ServiceDataModelExplicitServices",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelExplicitServices_ServiceDataModelId_CMDBProductId_Mode",
                table: "ServiceDataModelExplicitServices",
                columns: new[] { "ServiceDataModelId", "CMDBProductId", "Mode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelFieldOptions_ServiceDataModelFieldId_ValueKey",
                table: "ServiceDataModelFieldOptions",
                columns: new[] { "ServiceDataModelFieldId", "ValueKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelFields_ServiceDataModelGroupId_StableKey",
                table: "ServiceDataModelFields",
                columns: new[] { "ServiceDataModelGroupId", "StableKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelGroups_ServiceDataModelVersionId_StableKey",
                table: "ServiceDataModelGroups",
                columns: new[] { "ServiceDataModelVersionId", "StableKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelProposedRegisterChanges_ServiceDataModelAssignmentId",
                table: "ServiceDataModelProposedRegisterChanges",
                column: "ServiceDataModelAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelProposedRegisterChanges_ServiceDataModelFieldId",
                table: "ServiceDataModelProposedRegisterChanges",
                column: "ServiceDataModelFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelProposedRegisterChanges_Status",
                table: "ServiceDataModelProposedRegisterChanges",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModels_LifecycleStatus",
                table: "ServiceDataModels",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModels_StableKey",
                table: "ServiceDataModels",
                column: "StableKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelSubmissions_ServiceDataModelAssignmentId_IsCurrent",
                table: "ServiceDataModelSubmissions",
                columns: new[] { "ServiceDataModelAssignmentId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelSubmissions_ServiceDataModelAssignmentId_RevisionNumber",
                table: "ServiceDataModelSubmissions",
                columns: new[] { "ServiceDataModelAssignmentId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelVersions_ServiceDataModelId_VersionNumber",
                table: "ServiceDataModelVersions",
                columns: new[] { "ServiceDataModelId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDataModelVersions_Status",
                table: "ServiceDataModelVersions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceDataModelAnswers");

            migrationBuilder.DropTable(
                name: "ServiceDataModelApplicabilityRules");

            migrationBuilder.DropTable(
                name: "ServiceDataModelAuditEvents");

            migrationBuilder.DropTable(
                name: "ServiceDataModelExplicitServices");

            migrationBuilder.DropTable(
                name: "ServiceDataModelFieldOptions");

            migrationBuilder.DropTable(
                name: "ServiceDataModelProposedRegisterChanges");

            migrationBuilder.DropTable(
                name: "ServiceDataModelSubmissions");

            migrationBuilder.DropTable(
                name: "ServiceDataModelFields");

            migrationBuilder.DropTable(
                name: "ServiceDataModelAssignments");

            migrationBuilder.DropTable(
                name: "ServiceDataModelGroups");

            migrationBuilder.DropTable(
                name: "ServiceDataModelVersions");

            migrationBuilder.DropTable(
                name: "ServiceDataModels");
        }
    }
}
