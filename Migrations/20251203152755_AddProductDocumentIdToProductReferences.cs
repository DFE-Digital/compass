using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDocumentIdToProductReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductReturns_FipsId_Year_Month",
                table: "ProductReturns");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceReportingProductExclusions_FipsId_IsActive",
                table: "PerformanceReportingProductExclusions");

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "Risks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductFipsId",
                table: "ProjectProducts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "ProjectProducts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FipsId",
                table: "ProductReturns",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "ProductReturns",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FipsId",
                table: "ProductAccessibilities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "ProductAccessibilities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FipsId",
                table: "PerformanceReportingProductExclusions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "PerformanceReportingProductExclusions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "Milestones",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "Kpis",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductDocumentId",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ProductDocumentId",
                table: "Risks",
                column: "ProductDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProducts_ProductDocumentId",
                table: "ProjectProducts",
                column: "ProductDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReturns_FipsId",
                table: "ProductReturns",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReturns_ProductDocumentId_Year_Month",
                table: "ProductReturns",
                columns: new[] { "ProductDocumentId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAccessibilities_FipsId",
                table: "ProductAccessibilities",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAccessibilities_ProductDocumentId",
                table: "ProductAccessibilities",
                column: "ProductDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingProductExclusions_ProductDocumentId",
                table: "PerformanceReportingProductExclusions",
                column: "ProductDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingProductExclusions_ProductDocumentId_IsActive",
                table: "PerformanceReportingProductExclusions",
                columns: new[] { "ProductDocumentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProductDocumentId",
                table: "Milestones",
                column: "ProductDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_ProductDocumentId",
                table: "Kpis",
                column: "ProductDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ProductDocumentId",
                table: "Issues",
                column: "ProductDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ProductDocumentId",
                table: "Decisions",
                column: "ProductDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ProductDocumentId",
                table: "Actions",
                column: "ProductDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Risks_ProductDocumentId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectProducts_ProductDocumentId",
                table: "ProjectProducts");

            migrationBuilder.DropIndex(
                name: "IX_ProductReturns_FipsId",
                table: "ProductReturns");

            migrationBuilder.DropIndex(
                name: "IX_ProductReturns_ProductDocumentId_Year_Month",
                table: "ProductReturns");

            migrationBuilder.DropIndex(
                name: "IX_ProductAccessibilities_FipsId",
                table: "ProductAccessibilities");

            migrationBuilder.DropIndex(
                name: "IX_ProductAccessibilities_ProductDocumentId",
                table: "ProductAccessibilities");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceReportingProductExclusions_ProductDocumentId",
                table: "PerformanceReportingProductExclusions");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceReportingProductExclusions_ProductDocumentId_IsActive",
                table: "PerformanceReportingProductExclusions");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_ProductDocumentId",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_Kpis_ProductDocumentId",
                table: "Kpis");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ProductDocumentId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_ProductDocumentId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ProductDocumentId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "ProjectProducts");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "ProductReturns");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "ProductAccessibilities");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "PerformanceReportingProductExclusions");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "Kpis");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ProductDocumentId",
                table: "Actions");

            migrationBuilder.AlterColumn<string>(
                name: "ProductFipsId",
                table: "ProjectProducts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FipsId",
                table: "ProductReturns",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FipsId",
                table: "ProductAccessibilities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FipsId",
                table: "PerformanceReportingProductExclusions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReturns_FipsId_Year_Month",
                table: "ProductReturns",
                columns: new[] { "FipsId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingProductExclusions_FipsId_IsActive",
                table: "PerformanceReportingProductExclusions",
                columns: new[] { "FipsId", "IsActive" });
        }
    }
}
