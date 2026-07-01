using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Compass.ViewModels.Modern;

namespace Compass.Services.Raid;

/// <summary>Excel export for a single RAID register manage-view spreadsheets.</summary>
public static class RaidRegisterSpreadsheetExcelExport
{
    public static byte[] BuildWorkbook(RaidRegisterSpreadsheetExportModel model)
    {
        using var workbook = new XLWorkbook();
        WriteRisksSheet(workbook.AddWorksheet("Risks"), model.Risks);
        WriteIssuesSheet(workbook.AddWorksheet("Issues"), model.Issues);
        WriteAssumptionsSheet(workbook.AddWorksheet("Assumptions"), model.Assumptions);
        WriteNearMissesSheet(workbook.AddWorksheet("Near misses"), model.NearMisses);
        if (model.Dependencies.Count > 0)
            WriteDependenciesSheet(workbook.AddWorksheet("Dependencies"), model.Dependencies);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string BuildFileName(string registerName)
    {
        var stem = Regex.Replace((registerName ?? "").Trim(), @"[^\w\-]+", "-", RegexOptions.None, TimeSpan.FromSeconds(1))
            .Trim('-');
        if (string.IsNullOrEmpty(stem))
            stem = "raid-register";
        return $"{stem}-raid-register-{DateTime.UtcNow:yyyyMMdd}.xlsx";
    }

    private static void WriteRisksSheet(IXLWorksheet worksheet, IReadOnlyList<RaidRegisterRiskRow> rows)
    {
        var headers = new[]
        {
            "Ref", "Title", "Status", "Tier", "Relation", "Category", "Owner", "Description", "Cause", "Impact",
            "Contingency", "Assurance", "Financial impact", "KRIs", "Response strategy", "Mitigations",
            "Last comment / update",
            "Inherent impact", "Inherent likelihood", "Inherent score",
            "Current impact", "Current likelihood", "Current score",
            "Residual impact", "Residual likelihood", "Residual score",
            "Tolerance impact", "Tolerance likelihood", "Tolerance score",
            "Proximity", "Created date", "Last edited"
        };
        WriteHeaderRow(worksheet, headers);

        var rowNumber = 2;
        foreach (var risk in rows)
        {
            var col = 1;
            worksheet.Cell(rowNumber, col++).Value = risk.Reference;
            worksheet.Cell(rowNumber, col++).Value = risk.Title ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.Status ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.Tier ?? "";
            worksheet.Cell(rowNumber, col++).Value = FormatRelationText(risk.ToRelationParts("risks"));
            worksheet.Cell(rowNumber, col++).Value = risk.Category ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.Owner ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.Description ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.Cause ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.ImpactIfRealised ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.Contingency ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.Assurance ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.FinancialImpact ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.KrisSummary ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.Response ?? risk.ResponseStrategy ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.MitigationCount;
            worksheet.Cell(rowNumber, col++).Value = risk.LastCommentUpdateText ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.OriginalImpact ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.OriginalLikelihood ?? "";
            SetDecimalCell(worksheet.Cell(rowNumber, col++), risk.InherentScore);
            worksheet.Cell(rowNumber, col++).Value = risk.CurrentImpact ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.CurrentLikelihood ?? "";
            SetDecimalCell(worksheet.Cell(rowNumber, col++), risk.CurrentScore);
            worksheet.Cell(rowNumber, col++).Value = risk.ResidualImpact ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.ResidualLikelihood ?? "";
            SetDecimalCell(worksheet.Cell(rowNumber, col++), risk.ResidualScore);
            worksheet.Cell(rowNumber, col++).Value = risk.ToleranceImpact ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.ToleranceLikelihood ?? "";
            SetDecimalCell(worksheet.Cell(rowNumber, col++), risk.ToleranceScore);
            worksheet.Cell(rowNumber, col++).Value = risk.Proximity ?? "";
            worksheet.Cell(rowNumber, col++).Value = risk.CreatedAt;
            worksheet.Cell(rowNumber, col).Value = risk.UpdatedAt;
            rowNumber++;
        }

        FinalizeSheet(worksheet, headers.Length);
    }

    private static void WriteIssuesSheet(IXLWorksheet worksheet, IReadOnlyList<RaidRegisterIssueRow> rows)
    {
        var headers = new[]
        {
            "Ref", "Title", "Status", "Severity", "Priority", "Relation", "Category", "Owner", "Description",
            "Identified", "Target date", "Last edited"
        };
        WriteHeaderRow(worksheet, headers);

        var rowNumber = 2;
        foreach (var issue in rows)
        {
            var col = 1;
            worksheet.Cell(rowNumber, col++).Value = issue.Reference;
            worksheet.Cell(rowNumber, col++).Value = issue.Title ?? "";
            worksheet.Cell(rowNumber, col++).Value = issue.Status ?? "";
            worksheet.Cell(rowNumber, col++).Value = issue.Severity ?? "";
            worksheet.Cell(rowNumber, col++).Value = issue.Priority ?? "";
            worksheet.Cell(rowNumber, col++).Value = FormatRelationText(issue.ToRelationParts("issues"));
            worksheet.Cell(rowNumber, col++).Value = issue.Category ?? "";
            worksheet.Cell(rowNumber, col++).Value = issue.Owner ?? "";
            worksheet.Cell(rowNumber, col++).Value = issue.Description ?? "";
            SetDateCell(worksheet.Cell(rowNumber, col++), issue.IdentifiedDate);
            SetDateCell(worksheet.Cell(rowNumber, col++), issue.TargetResolutionDate);
            worksheet.Cell(rowNumber, col).Value = issue.UpdatedAt;
            rowNumber++;
        }

        FinalizeSheet(worksheet, headers.Length);
    }

    private static void WriteAssumptionsSheet(IXLWorksheet worksheet, IReadOnlyList<RaidRegisterAssumptionRow> rows)
    {
        var headers = new[]
        {
            "Ref", "Description", "Status", "Criticality", "Relation", "Owner", "Created date", "Last edited"
        };
        WriteHeaderRow(worksheet, headers);

        var rowNumber = 2;
        foreach (var assumption in rows)
        {
            var col = 1;
            worksheet.Cell(rowNumber, col++).Value = assumption.Reference;
            worksheet.Cell(rowNumber, col++).Value = assumption.Description ?? "";
            worksheet.Cell(rowNumber, col++).Value = assumption.Status ?? "";
            worksheet.Cell(rowNumber, col++).Value = assumption.Criticality ?? "";
            worksheet.Cell(rowNumber, col++).Value = FormatRelationText(assumption.ToRelationParts("assumptions"));
            worksheet.Cell(rowNumber, col++).Value = assumption.Owner ?? "";
            worksheet.Cell(rowNumber, col++).Value = assumption.CreatedAt;
            worksheet.Cell(rowNumber, col).Value = assumption.UpdatedAt;
            rowNumber++;
        }

        FinalizeSheet(worksheet, headers.Length);
    }

    private static void WriteNearMissesSheet(IXLWorksheet worksheet, IReadOnlyList<RaidRegisterNearMissRow> rows)
    {
        var headers = new[]
        {
            "Ref", "Impact", "Status", "Seriousness", "Type", "Scope", "Date logged", "Last edited"
        };
        WriteHeaderRow(worksheet, headers);

        var rowNumber = 2;
        foreach (var nearMiss in rows)
        {
            var col = 1;
            worksheet.Cell(rowNumber, col++).Value = nearMiss.Reference;
            worksheet.Cell(rowNumber, col++).Value = nearMiss.Impact ?? "";
            worksheet.Cell(rowNumber, col++).Value = nearMiss.Status ?? "";
            worksheet.Cell(rowNumber, col++).Value = nearMiss.Seriousness ?? "";
            worksheet.Cell(rowNumber, col++).Value = nearMiss.Type ?? "";
            worksheet.Cell(rowNumber, col++).Value = FormatNearMissScope(nearMiss.ToRelationParts("nearmisses"));
            SetDateCell(worksheet.Cell(rowNumber, col++), nearMiss.DateLogged);
            worksheet.Cell(rowNumber, col).Value = nearMiss.UpdatedAt;
            rowNumber++;
        }

        FinalizeSheet(worksheet, headers.Length);
    }

    private static void WriteDependenciesSheet(IXLWorksheet worksheet, IReadOnlyList<RaidRegisterDependencyRow> rows)
    {
        var headers = new[] { "Description", "Link type", "Status", "Owner" };
        WriteHeaderRow(worksheet, headers);

        var rowNumber = 2;
        foreach (var dependency in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = dependency.Description ?? "";
            worksheet.Cell(rowNumber, 2).Value = dependency.LinkType ?? "";
            worksheet.Cell(rowNumber, 3).Value = dependency.Status ?? "";
            worksheet.Cell(rowNumber, 4).Value = dependency.Owner ?? "";
            rowNumber++;
        }

        FinalizeSheet(worksheet, headers.Length);
    }

    private static string FormatNearMissScope(RaidRegisterRelationParts rel)
    {
        if (rel.Kind == RaidRegisterRelationKinds.Organisation)
        {
            var target = (rel.Target ?? "").Trim();
            return string.IsNullOrEmpty(target) ? "Organisation" : $"Organisation · {target}";
        }

        return "—";
    }

    private static string FormatRelationText(RaidRegisterRelationParts rel)
    {
        if (rel.Kind == RaidRegisterRelationKinds.Organisation)
        {
            var target = (rel.Target ?? "").Trim();
            return string.IsNullOrEmpty(target)
                ? "Not linked to a work item or service"
                : $"Organisation · {target}";
        }

        var name = (rel.RelatedTitle ?? rel.Target ?? "").Trim();
        return string.IsNullOrEmpty(name) ? "—" : name;
    }

    private static void WriteHeaderRow(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];
    }

    private static void FinalizeSheet(IXLWorksheet worksheet, int columnCount)
    {
        worksheet.Range(1, 1, 1, columnCount).Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns(1, columnCount).AdjustToContents();
    }

    private static void SetDecimalCell(IXLCell cell, decimal? value)
    {
        if (value.HasValue)
            cell.Value = value.Value;
        else
            cell.Value = "";
    }

    private static void SetDateCell(IXLCell cell, DateTime? value)
    {
        if (value.HasValue)
            cell.Value = value.Value;
        else
            cell.Value = "";
    }
}
