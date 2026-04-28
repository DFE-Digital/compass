using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddPipelineDemandRequestIdToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PipelineDemandRequestId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DemandPipelineTriageMeetings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PipelineDemandRequestId",
                table: "Projects",
                column: "PipelineDemandRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_DemandPipelineRequests_PipelineDemandRequestId",
                table: "Projects",
                column: "PipelineDemandRequestId",
                principalTable: "DemandPipelineRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_DemandPipelineRequests_PipelineDemandRequestId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_PipelineDemandRequestId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PipelineDemandRequestId",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DemandPipelineTriageMeetings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);
        }
    }
}
