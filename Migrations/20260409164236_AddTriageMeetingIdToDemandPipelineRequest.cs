using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageMeetingIdToDemandPipelineRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TriageMeetingId",
                table: "DemandPipelineRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequests_TriageMeetingId",
                table: "DemandPipelineRequests",
                column: "TriageMeetingId");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandPipelineRequests_DemandPipelineTriageMeetings_TriageMeetingId",
                table: "DemandPipelineRequests",
                column: "TriageMeetingId",
                principalTable: "DemandPipelineTriageMeetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandPipelineRequests_DemandPipelineTriageMeetings_TriageMeetingId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropIndex(
                name: "IX_DemandPipelineRequests_TriageMeetingId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "TriageMeetingId",
                table: "DemandPipelineRequests");
        }
    }
}
