using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversalBarriersAndExploreRelatedLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExploreRelatedLinksJson",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UniversalBarrierLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    GuidanceUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversalBarrierLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandPipelineRequestUniversalBarriers",
                columns: table => new
                {
                    DemandPipelineRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversalBarrierLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandPipelineRequestUniversalBarriers", x => new { x.DemandPipelineRequestId, x.UniversalBarrierLookupId });
                    table.ForeignKey(
                        name: "FK_DemandPipelineRequestUniversalBarriers_DemandPipelineRequests_DemandPipelineRequestId",
                        column: x => x.DemandPipelineRequestId,
                        principalTable: "DemandPipelineRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DemandPipelineRequestUniversalBarriers_UniversalBarrierLookups_UniversalBarrierLookupId",
                        column: x => x.UniversalBarrierLookupId,
                        principalTable: "UniversalBarrierLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequestUniversalBarriers_UniversalBarrierLookupId",
                table: "DemandPipelineRequestUniversalBarriers",
                column: "UniversalBarrierLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_UniversalBarrierLookups_SortOrder",
                table: "UniversalBarrierLookups",
                column: "SortOrder");

            var guidanceUrl = "https://www.gov.uk/service-manual/design/making-your-service-more-inclusive#universal-barriers-to-using-a-service";
            var seedUtc = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "UniversalBarrierLookups",
                columns: new[] { "Name", "Description", "GuidanceUrl", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "Awareness", "Users need to know your service exists (for example without internet, or where information is not easy to understand in English).", guidanceUrl, 1, true, seedUtc, seedUtc },
                    { "Device and interaction skills", "Users may not be able to use devices or communicate easily (for example poor phone signal affecting call centres).", guidanceUrl, 2, true, seedUtc, seedUtc },
                    { "Time", "Users may not have time to gather information, complete forms, travel, or wait on the phone.", guidanceUrl, 3, true, seedUtc, seedUtc },
                    { "Enthusiasm", "The effort to use the service may feel greater than the benefit (for example reluctance to move from an offline service).", guidanceUrl, 4, true, seedUtc, seedUtc },
                    { "Access", "Users may be excluded if they must use something hard to access (place, channel, device — for example no printer, email access, or reliable transport).", guidanceUrl, 5, true, seedUtc, seedUtc },
                    { "Comprehension skills", "Users may need spoken or written English skills they do not have (for example low literacy or non-native English).", guidanceUrl, 6, true, seedUtc, seedUtc },
                    { "Evidence", "Not everyone has the same proof or identity documents (for example passport, driving licence, fixed address).", guidanceUrl, 7, true, seedUtc, seedUtc },
                    { "Self confidence", "Complex services can exclude users who doubt their ability to complete tasks (for example lower digital confidence).", guidanceUrl, 8, true, seedUtc, seedUtc },
                    { "Finance", "Fees, call or travel costs, or lost income from time off can exclude people.", guidanceUrl, 9, true, seedUtc, seedUtc },
                    { "Trust", "Users must trust technology and people in the service (for example security of payments or past negative experiences).", guidanceUrl, 10, true, seedUtc, seedUtc },
                    { "Emotional state", "Stress, tiredness, or distress may prevent users completing the task.", guidanceUrl, 11, true, seedUtc, seedUtc }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandPipelineRequestUniversalBarriers");

            migrationBuilder.DropTable(
                name: "UniversalBarrierLookups");

            migrationBuilder.DropColumn(
                name: "ExploreRelatedLinksJson",
                table: "DemandPipelineRequests");
        }
    }
}
