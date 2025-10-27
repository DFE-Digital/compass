using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddWcagCriteriaLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WcagVersion",
                table: "AccessibilityIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "WcagLevel",
                table: "AccessibilityIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "WcagCriteria",
                table: "AccessibilityIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "IssueType",
                table: "AccessibilityIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "WcagCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Criterion = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Level = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WcagCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueWcagCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessibilityIssueId = table.Column<int>(type: "int", nullable: false),
                    WcagCriterionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueWcagCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueWcagCriteria_AccessibilityIssues_AccessibilityIssueId",
                        column: x => x.AccessibilityIssueId,
                        principalTable: "AccessibilityIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueWcagCriteria_WcagCriteria_WcagCriterionId",
                        column: x => x.WcagCriterionId,
                        principalTable: "WcagCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueWcagCriteria_AccessibilityIssueId",
                table: "IssueWcagCriteria",
                column: "AccessibilityIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueWcagCriteria_WcagCriterionId",
                table: "IssueWcagCriteria",
                column: "WcagCriterionId");

            // Seed WCAG 2.2 criteria
            var now = DateTime.UtcNow;
            migrationBuilder.InsertData(
                table: "WcagCriteria",
                columns: new[] { "Criterion", "Title", "Level", "Version", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt", "Url" },
                values: new object[,]
                {
                    // Perceivable - Principle 1
                    { "1.1.1", "Non-text Content", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/non-text-content.html" },
                    { "1.2.1", "Audio-only and Video-only (Prerecorded)", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/audio-only-and-video-only-prerecorded.html" },
                    { "1.2.2", "Captions (Prerecorded)", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/captions-prerecorded.html" },
                    { "1.2.3", "Audio Description or Media Alternative", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/audio-description-or-media-alternative-prerecorded.html" },
                    { "1.2.4", "Captions (Live)", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/captions-live.html" },
                    { "1.2.5", "Audio Description", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/audio-description-prerecorded.html" },
                    { "1.3.1", "Info and Relationships", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/info-and-relationships.html" },
                    { "1.3.2", "Meaningful Sequence", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/meaningful-sequence.html" },
                    { "1.3.3", "Sensory Characteristics", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/sensory-characteristics.html" },
                    { "1.3.4", "Orientation", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/orientation.html" },
                    { "1.3.5", "Identify Input Purpose", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/identify-input-purpose.html" },
                    { "1.4.1", "Use of Color", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html" },
                    { "1.4.2", "Audio Control", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/audio-control.html" },
                    { "1.4.3", "Contrast (Minimum)", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html" },
                    { "1.4.4", "Resize Text", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/resize-text.html" },
                    { "1.4.5", "Images of Text", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/images-of-text.html" },
                    { "1.4.6", "Contrast (Enhanced)", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/contrast-enhanced.html" },
                    { "1.4.7", "Low or No Background Audio", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/low-or-no-background-audio.html" },
                    { "1.4.8", "Visual Presentation", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/visual-presentation.html" },
                    { "1.4.9", "Images of Text (No Exception)", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/images-of-text-no-exception.html" },
                    { "1.4.10", "Reflow", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/reflow.html" },
                    { "1.4.11", "Non-text Contrast", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html" },
                    { "1.4.12", "Text Spacing", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/text-spacing.html" },
                    { "1.4.13", "Content on Hover or Focus", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/content-on-hover-or-focus.html" },
                    // Operable - Principle 2
                    { "2.1.1", "Keyboard", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/keyboard.html" },
                    { "2.1.2", "No Keyboard Trap", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/no-keyboard-trap.html" },
                    { "2.1.3", "Keyboard (No Exception)", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/keyboard-no-exception.html" },
                    { "2.1.4", "Character Key Shortcuts", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/character-key-shortcuts.html" },
                    { "2.2.1", "Timing Adjustable", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/timing-adjustable.html" },
                    { "2.2.2", "Pause, Stop, Hide", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/pause-stop-hide.html" },
                    { "2.2.3", "No Timing", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/no-timing.html" },
                    { "2.2.4", "Interruptions", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/interruptions.html" },
                    { "2.2.5", "Re-authenticating", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/re-authenticating.html" },
                    { "2.2.6", "Timeouts", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/timeouts.html" },
                    { "2.3.1", "Three Flashes or Below", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/three-flashes-or-below-threshold.html" },
                    { "2.3.2", "Three Flashes", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/three-flashes.html" },
                    { "2.3.3", "Animation from Interactions", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/animation-from-interactions.html" },
                    { "2.4.1", "Bypass Blocks", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/bypass-blocks.html" },
                    { "2.4.2", "Page Titled", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/page-titled.html" },
                    { "2.4.3", "Focus Order", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/focus-order.html" },
                    { "2.4.4", "Link Purpose (In Context)", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/link-purpose-in-context.html" },
                    { "2.4.5", "Multiple Ways", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/multiple-ways.html" },
                    { "2.4.6", "Headings and Labels", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/headings-and-labels.html" },
                    { "2.4.7", "Focus Visible", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/focus-visible.html" },
                    { "2.4.8", "Location", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/location.html" },
                    { "2.4.9", "Link Purpose (Link Only)", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/link-purpose-link-only.html" },
                    { "2.4.10", "Section Headings", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/section-headings.html" },
                    { "2.4.11", "Focus Not Obscured (Minimum)", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-minimum.html" },
                    { "2.4.12", "Focus Not Obscured (Enhanced)", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-enhanced.html" },
                    { "2.4.13", "Focus Appearance", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/focus-appearance.html" },
                    { "2.5.7", "Dragging Movements", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/dragging-movements.html" },
                    { "2.5.8", "Target Size (Minimum)", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html" },
                    { "2.5.6", "Concurrent Input Mechanisms", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/concurrent-input-mechanisms.html" },
                    // Understandable - Principle 3
                    { "3.1.1", "Language of Page", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/language-of-page.html" },
                    { "3.1.2", "Language of Parts", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/language-of-parts.html" },
                    { "3.1.3", "Unusual Words", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/unusual-words.html" },
                    { "3.1.4", "Abbreviations", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/abbreviations.html" },
                    { "3.1.5", "Reading Level", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/reading-level.html" },
                    { "3.1.6", "Pronunciation", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/pronunciation.html" },
                    { "3.2.1", "On Focus", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/on-focus.html" },
                    { "3.2.2", "On Input", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/on-input.html" },
                    { "3.2.3", "Consistent Navigation", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/consistent-navigation.html" },
                    { "3.2.4", "Consistent Identification", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/consistent-identification.html" },
                    { "3.2.5", "Change on Request", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/change-on-request.html" },
                    { "3.2.6", "Consistent Help", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/consistent-help.html" },
                    { "3.3.1", "Error Identification", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/error-identification.html" },
                    { "3.3.2", "Labels or Instructions", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/labels-or-instructions.html" },
                    { "3.3.3", "Error Suggestion", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/error-suggestion.html" },
                    { "3.3.4", "Error Prevention", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/error-prevention-legal-financial-data.html" },
                    { "3.3.5", "Help", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/help.html" },
                    { "3.3.6", "Error Prevention (All)", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/error-prevention-all.html" },
                    { "3.3.7", "Redundant Entry", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/redundant-entry.html" },
                    { "3.3.8", "Accessible Authentication", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/accessible-authentication-minimum.html" },
                    { "3.3.9", "Accessible Authentication (Enhanced)", "AAA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/accessible-authentication-enhanced.html" },
                    // Robust - Principle 4
                    { "4.1.1", "Parsing", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/parsing.html" },
                    { "4.1.2", "Name, Role, Value", "A", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/name-role-value.html" },
                    { "4.1.3", "Status Messages", "AA", "2.2", 0, true, now, now, "https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueWcagCriteria");

            migrationBuilder.DropTable(
                name: "WcagCriteria");

            migrationBuilder.DropColumn(
                name: "IssueType",
                table: "AccessibilityIssues");

            migrationBuilder.AlterColumn<string>(
                name: "WcagVersion",
                table: "AccessibilityIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WcagLevel",
                table: "AccessibilityIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WcagCriteria",
                table: "AccessibilityIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
