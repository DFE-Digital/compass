using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFipsCmdbProductTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CMDBProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniqueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CMDBDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    UserDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CMDBID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProductURL = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PhaseId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProducts_PhaseLookups_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "PhaseLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FipsBusinessAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsBusinessAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FipsChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FipsContactRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AllowMultiple = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsContactRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FipsTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FipsUserGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsUserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FipsUserGroups_FipsUserGroups_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FipsUserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductBusinessAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FipsBusinessAreaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductBusinessAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductBusinessAreas_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductBusinessAreas_FipsBusinessAreas_FipsBusinessAreaId",
                        column: x => x.FipsBusinessAreaId,
                        principalTable: "FipsBusinessAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FipsChannelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductChannels_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductChannels_FipsChannels_FipsChannelId",
                        column: x => x.FipsChannelId,
                        principalTable: "FipsChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FipsContactRoleId = table.Column<int>(type: "int", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CanManage = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductContacts_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductContacts_FipsContactRoles_FipsContactRoleId",
                        column: x => x.FipsContactRoleId,
                        principalTable: "FipsContactRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FipsTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductTypes_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductTypes_FipsTypes_FipsTypeId",
                        column: x => x.FipsTypeId,
                        principalTable: "FipsTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductUserGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FipsUserGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductUserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductUserGroups_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductUserGroups_FipsUserGroups_FipsUserGroupId",
                        column: x => x.FipsUserGroupId,
                        principalTable: "FipsUserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FipsUserGroupSynonyms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FipsUserGroupId = table.Column<int>(type: "int", nullable: false),
                    Synonym = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsUserGroupSynonyms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FipsUserGroupSynonyms_FipsUserGroups_FipsUserGroupId",
                        column: x => x.FipsUserGroupId,
                        principalTable: "FipsUserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductBusinessAreas_CMDBProductId",
                table: "CMDBProductBusinessAreas",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductBusinessAreas_FipsBusinessAreaId",
                table: "CMDBProductBusinessAreas",
                column: "FipsBusinessAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductChannels_CMDBProductId",
                table: "CMDBProductChannels",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductChannels_FipsChannelId",
                table: "CMDBProductChannels",
                column: "FipsChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductContacts_CMDBProductId",
                table: "CMDBProductContacts",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductContacts_FipsContactRoleId",
                table: "CMDBProductContacts",
                column: "FipsContactRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProducts_PhaseId",
                table: "CMDBProducts",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProducts_Status",
                table: "CMDBProducts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProducts_UniqueID",
                table: "CMDBProducts",
                column: "UniqueID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductTypes_CMDBProductId",
                table: "CMDBProductTypes",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductTypes_FipsTypeId",
                table: "CMDBProductTypes",
                column: "FipsTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductUserGroups_CMDBProductId",
                table: "CMDBProductUserGroups",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductUserGroups_FipsUserGroupId",
                table: "CMDBProductUserGroups",
                column: "FipsUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FipsUserGroups_ParentId",
                table: "FipsUserGroups",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_FipsUserGroupSynonyms_FipsUserGroupId",
                table: "FipsUserGroupSynonyms",
                column: "FipsUserGroupId");

            migrationBuilder.Sql(@"
INSERT INTO FipsContactRoles (Name, Description, AllowMultiple, DisplayOrder, Active) VALUES
('Product manager', 'The product manager responsible for this product', 0, 10, 1),
('Service Owner', 'The service owner accountable for this product', 0, 20, 1),
('Senior Responsible Officer', 'The SRO with overall accountability', 0, 30, 1),
('Information Asset Owner', 'The IAO responsible for information assets', 0, 40, 1),
('Delivery Manager', 'The delivery manager for this product', 0, 50, 1),
('Reporting contact', 'Contact for reporting purposes', 1, 60, 1);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CMDBProductBusinessAreas");

            migrationBuilder.DropTable(
                name: "CMDBProductChannels");

            migrationBuilder.DropTable(
                name: "CMDBProductContacts");

            migrationBuilder.DropTable(
                name: "CMDBProductTypes");

            migrationBuilder.DropTable(
                name: "CMDBProductUserGroups");

            migrationBuilder.DropTable(
                name: "FipsUserGroupSynonyms");

            migrationBuilder.DropTable(
                name: "FipsBusinessAreas");

            migrationBuilder.DropTable(
                name: "FipsChannels");

            migrationBuilder.DropTable(
                name: "FipsContactRoles");

            migrationBuilder.DropTable(
                name: "FipsTypes");

            migrationBuilder.DropTable(
                name: "CMDBProducts");

            migrationBuilder.DropTable(
                name: "FipsUserGroups");
        }
    }
}
