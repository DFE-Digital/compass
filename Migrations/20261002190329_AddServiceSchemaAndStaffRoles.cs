using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceSchemaAndStaffRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StaffRoleId",
                table: "ProjectContacts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CensusCatalogueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Statement = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    LookupCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensusCatalogueItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CensusSectionDeclarations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensusSectionDeclarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CensusSectionDeclarations_CMDBProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CensusServiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    BoundaryIn = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    BoundaryOut = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensusServiceLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CensusServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PurposeNarrative = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    BoundaryIn = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    BoundaryOut = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ServiceTypeCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensusServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSchemaLookupSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSchemaLookupSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Family = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFrameworkRole = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CensusServiceLineServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationshipCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensusServiceLineServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CensusServiceLineServices_CensusServiceLines_ServiceLineId",
                        column: x => x.ServiceLineId,
                        principalTable: "CensusServiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CensusServiceLineServices_CensusServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "CensusServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CensusServiceProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationshipCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Narrative = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensusServiceProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CensusServiceProducts_CMDBProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CensusServiceProducts_CensusServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "CensusServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSchemaLookupValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LookupSetId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSchemaLookupValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceSchemaLookupValues_ServiceSchemaLookupSets_LookupSetId",
                        column: x => x.LookupSetId,
                        principalTable: "ServiceSchemaLookupSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CensusEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DomainKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Narrative = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    LookupCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SecondaryLookupCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CatalogueItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StaffRoleId = table.Column<int>(type: "int", nullable: true),
                    PersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PersonEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerificationStatusCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RemovalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensusEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CensusEntries_CMDBProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CensusEntries_CensusCatalogueItems_CatalogueItemId",
                        column: x => x.CatalogueItemId,
                        principalTable: "CensusCatalogueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CensusEntries_StaffRoles_StaffRoleId",
                        column: x => x.StaffRoleId,
                        principalTable: "StaffRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContacts_StaffRoleId",
                table: "ProjectContacts",
                column: "StaffRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CensusCatalogueItems_Kind_Reference",
                table: "CensusCatalogueItems",
                columns: new[] { "Kind", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CensusEntries_CatalogueItemId",
                table: "CensusEntries",
                column: "CatalogueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CensusEntries_ProductId_DomainKey",
                table: "CensusEntries",
                columns: new[] { "ProductId", "DomainKey" });

            migrationBuilder.CreateIndex(
                name: "IX_CensusEntries_StaffRoleId",
                table: "CensusEntries",
                column: "StaffRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CensusSectionDeclarations_ProductId_SectionKey",
                table: "CensusSectionDeclarations",
                columns: new[] { "ProductId", "SectionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CensusServiceLines_Reference",
                table: "CensusServiceLines",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CensusServiceLineServices_ServiceId",
                table: "CensusServiceLineServices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CensusServiceLineServices_ServiceLineId",
                table: "CensusServiceLineServices",
                column: "ServiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CensusServiceProducts_ProductId",
                table: "CensusServiceProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CensusServiceProducts_ServiceId_ProductId_RelationshipCode",
                table: "CensusServiceProducts",
                columns: new[] { "ServiceId", "ProductId", "RelationshipCode" });

            migrationBuilder.CreateIndex(
                name: "IX_CensusServices_Reference",
                table: "CensusServices",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSchemaLookupSets_Key",
                table: "ServiceSchemaLookupSets",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSchemaLookupValues_LookupSetId_Code",
                table: "ServiceSchemaLookupValues",
                columns: new[] { "LookupSetId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffRoles_Code",
                table: "StaffRoles",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectContacts_StaffRoles_StaffRoleId",
                table: "ProjectContacts",
                column: "StaffRoleId",
                principalTable: "StaffRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectContacts_StaffRoles_StaffRoleId",
                table: "ProjectContacts");

            migrationBuilder.DropTable(
                name: "CensusEntries");

            migrationBuilder.DropTable(
                name: "CensusSectionDeclarations");

            migrationBuilder.DropTable(
                name: "CensusServiceLineServices");

            migrationBuilder.DropTable(
                name: "CensusServiceProducts");

            migrationBuilder.DropTable(
                name: "ServiceSchemaLookupValues");

            migrationBuilder.DropTable(
                name: "CensusCatalogueItems");

            migrationBuilder.DropTable(
                name: "StaffRoles");

            migrationBuilder.DropTable(
                name: "CensusServiceLines");

            migrationBuilder.DropTable(
                name: "CensusServices");

            migrationBuilder.DropTable(
                name: "ServiceSchemaLookupSets");

            migrationBuilder.DropIndex(
                name: "IX_ProjectContacts_StaffRoleId",
                table: "ProjectContacts");

            migrationBuilder.DropColumn(
                name: "StaffRoleId",
                table: "ProjectContacts");
        }
    }
}
