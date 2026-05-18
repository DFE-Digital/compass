using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddNearMisses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NearMissSeriousnesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearMissSeriousnesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NearMissStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearMissStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NearMissTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearMissTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NearMisses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateLogged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NearMissTypeId = table.Column<int>(type: "int", nullable: true),
                    DirectorateLookupId = table.Column<int>(type: "int", nullable: true),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: true),
                    Impact = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NearMissSeriousnessId = table.Column<int>(type: "int", nullable: true),
                    PostMitigationRagStatusLookupId = table.Column<int>(type: "int", nullable: true),
                    RiskTierId = table.Column<int>(type: "int", nullable: true),
                    NearMissStatusId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearMisses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NearMisses_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NearMisses_DirectorateLookups_DirectorateLookupId",
                        column: x => x.DirectorateLookupId,
                        principalTable: "DirectorateLookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NearMisses_NearMissSeriousnesses_NearMissSeriousnessId",
                        column: x => x.NearMissSeriousnessId,
                        principalTable: "NearMissSeriousnesses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NearMisses_NearMissStatuses_NearMissStatusId",
                        column: x => x.NearMissStatusId,
                        principalTable: "NearMissStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NearMisses_NearMissTypes_NearMissTypeId",
                        column: x => x.NearMissTypeId,
                        principalTable: "NearMissTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NearMisses_RagStatusLookups_PostMitigationRagStatusLookupId",
                        column: x => x.PostMitigationRagStatusLookupId,
                        principalTable: "RagStatusLookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NearMisses_RiskTiers_RiskTierId",
                        column: x => x.RiskTierId,
                        principalTable: "RiskTiers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NearMisses_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NearMisses_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NearMissActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NearMissId = table.Column<int>(type: "int", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearMissActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NearMissActions_NearMisses_NearMissId",
                        column: x => x.NearMissId,
                        principalTable: "NearMisses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NearMissActions_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NearMissMitigations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NearMissId = table.Column<int>(type: "int", nullable: false),
                    MitigationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssuranceTakenPlace = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearMissMitigations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NearMissMitigations_NearMisses_NearMissId",
                        column: x => x.NearMissId,
                        principalTable: "NearMisses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NearMissMitigations_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NearMissOwners",
                columns: table => new
                {
                    NearMissId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearMissOwners", x => new { x.NearMissId, x.UserId });
                    table.ForeignKey(
                        name: "FK_NearMissOwners_NearMisses_NearMissId",
                        column: x => x.NearMissId,
                        principalTable: "NearMisses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NearMissOwners_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NearMissActions_NearMissId",
                table: "NearMissActions",
                column: "NearMissId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMissActions_RecordedByUserId",
                table: "NearMissActions",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_BusinessAreaLookupId",
                table: "NearMisses",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_CreatedByUserId",
                table: "NearMisses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_DirectorateLookupId",
                table: "NearMisses",
                column: "DirectorateLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_NearMissSeriousnessId",
                table: "NearMisses",
                column: "NearMissSeriousnessId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_NearMissStatusId",
                table: "NearMisses",
                column: "NearMissStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_NearMissTypeId",
                table: "NearMisses",
                column: "NearMissTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_PostMitigationRagStatusLookupId",
                table: "NearMisses",
                column: "PostMitigationRagStatusLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_Reference",
                table: "NearMisses",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_RiskTierId",
                table: "NearMisses",
                column: "RiskTierId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMisses_UpdatedByUserId",
                table: "NearMisses",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMissMitigations_NearMissId",
                table: "NearMissMitigations",
                column: "NearMissId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMissMitigations_RecordedByUserId",
                table: "NearMissMitigations",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NearMissOwners_UserId",
                table: "NearMissOwners",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NearMissActions");

            migrationBuilder.DropTable(
                name: "NearMissMitigations");

            migrationBuilder.DropTable(
                name: "NearMissOwners");

            migrationBuilder.DropTable(
                name: "NearMisses");

            migrationBuilder.DropTable(
                name: "NearMissSeriousnesses");

            migrationBuilder.DropTable(
                name: "NearMissStatuses");

            migrationBuilder.DropTable(
                name: "NearMissTypes");
        }
    }
}
