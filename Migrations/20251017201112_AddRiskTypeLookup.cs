using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskTypeLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiskTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskTypes_Code",
                table: "RiskTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskTypes_IsActive",
                table: "RiskTypes",
                column: "IsActive");

            // Seed Risk Types
            var now = DateTime.UtcNow;
            
            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "STRATEGY", "Strategy risk", "Risks arising from identifying and pursuing a strategy that is poorly defined, based on flawed or inaccurate data, or fails to support the delivery of commitments, plans, or objectives due to a changing macro-environment (political, economic, social, technological, environmental, or legislative change).", "Poorly defined or outdated strategic direction.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "GOVERNANCE", "Governance risk", "Risks arising from unclear plans, priorities, authorities, and accountabilities, and/or ineffective or disproportionate oversight of decision-making or performance.", "Weak oversight, unclear accountability, or poor governance.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "OPERATIONS", "Operations risk", "Risks arising from inadequate, poorly designed, ineffective, or inefficient internal processes resulting in fraud, error, impaired customer service, non-compliance, or poor value for money.", "Failures in internal processes or delivery operations.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "LEGAL", "Legal risk", "Risks arising from a defective transaction, a claim being made, or other legal events that result in liability or loss, or from a failure to meet legal or regulatory requirements or to protect assets (e.g. intellectual property).", "Exposure to legal, regulatory, or contractual liabilities.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "PROPERTY", "Property risk", "Risks arising from property deficiencies or poorly designed or ineffective safety management leading to non-compliance, harm, or injury to employees, contractors, service users, or the public.", "Property, estate, or safety management deficiencies.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "FINANCIAL", "Financial risk", "Risks arising from failure to manage finances in line with requirements, leading to poor returns, unmanaged assets or liabilities, lack of value for money, or non-compliant reporting.", "Ineffective financial control or value-for-money failure.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "COMMERCIAL", "Commercial risk", "Risks arising from weaknesses in the management of commercial partnerships, supply chains, or contracts, resulting in poor performance, inefficiency, fraud, or unmet business objectives.", "Failures in contracts, suppliers, or commercial management.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "PEOPLE", "People risk", "Risks arising from ineffective leadership, poor culture, lack of capability or capacity, inappropriate behaviours, industrial action, or breaches of employment law or HR policy.", "Leadership, culture, or workforce capacity/capability issues.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "TECHNOLOGY", "Technology risk", "Risks arising from technology not delivering the expected services due to inadequate or deficient system design, development, performance, or resilience.", "Technology failures or insufficient system resilience.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "INFORMATION", "Information risk", "Risks arising from a failure to produce or use robust, suitable, and appropriate data and information, or to exploit data to its full potential.", "Data quality, availability, or misuse issues.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "SECURITY", "Security risk", "Risks arising from a failure to prevent unauthorised or inappropriate access to information or assets, including cyber security breaches or GDPR non-compliance.", "Cyber, data, or physical security breaches.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "PROJECT", "Project/Programme risk", "Risks that programmes and projects are not aligned with strategic priorities, or fail to deliver intended benefits to time, cost, or quality.", "Programme or project delivery failures.", true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTypes",
                columns: new[] { "Code", "Name", "Description", "Summary", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "REPUTATIONAL", "Reputational risk", "Risks arising from adverse events, ethical breaches, sustainability failures, or poor quality or innovation leading to loss of reputation or trust.", "Loss of stakeholder trust or public confidence.", true, now, now });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskTypes");
        }
    }
}
