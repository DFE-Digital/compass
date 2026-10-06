using ClosedXML.Excel;

namespace Compass.Services.Modern;

/// <summary>
/// Builds a GDS Performance Commission workbook with Context / Service definition and input sheets.
/// Column names and order match the DfE performance commission template.
/// </summary>
public static class GdsReturnExcelExport
{
    public static byte[] BuildWorkbook(
        IReadOnlyList<IReadOnlyList<string>> contextRows,
        IReadOnlyList<IReadOnlyList<string>> serviceRows)
    {
        using var workbook = new XLWorkbook();

        WriteContextDefinitions(workbook.Worksheets.Add("Context Data Definitions"));
        WriteInputSheet(
            workbook.Worksheets.Add("Context Data Input"),
            GdsReturnMapping.ContextDataColumns,
            contextRows);
        WriteServiceDefinitions(workbook.Worksheets.Add("Service Data Definitions"));
        WriteInputSheet(
            workbook.Worksheets.Add("Service Data Input"),
            GdsReturnMapping.ServiceDataColumns,
            serviceRows);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteInputSheet(
        IXLWorksheet ws,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        for (var col = 0; col < headers.Count; col++)
        {
            var cell = ws.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var col = 0; col < headers.Count; col++)
            {
                var value = col < row.Count ? row[col] : "";
                var cell = ws.Cell(r + 2, col + 1);
                cell.Style.NumberFormat.Format = "@";
                cell.Value = value ?? "";
            }
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void WriteContextDefinitions(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "Dataset information for rAPId";
        ws.Cell(2, 1).Value = "Layer";
        ws.Cell(2, 2).Value = "Domain";
        ws.Cell(2, 3).Value = "Dataset";
        ws.Cell(3, 1).Value = "raw_data";
        ws.Cell(3, 2).Value = "digital_performance_dashboard_dfe";
        ws.Cell(3, 3).Value = "context_data_upload_performance_dashboard";

        ws.Cell(5, 1).Value = "Column Name";
        ws.Cell(5, 2).Value = "Description";
        ws.Cell(5, 3).Value = "Format";
        ws.Cell(5, 4).Value = "Can be empty";
        ws.Row(5).Style.Font.Bold = true;

        var defs = new (string Name, string Description, string Format, string CanBeEmpty)[]
        {
            ("service_name", "The name of the service. This should match the published name of the service, as well as the Service Data tab.", "Free text", "No"),
            ("transactional_or_not", "Is this service transactional? A transactional service allows users to complete an action or transaction. Examples include Apply for a licence, Submit a claim, or Make a payment.", "Options are: \"Yes\" or \"No\"", "Yes"),
            ("service_status", "What is the status of the service? These are outlined in the Service Manual. Please do not report on retired services unless they have been requested.", "Options are: \"Live\", \"Public beta\", \"Private beta\", \"Alpha\" or \"Retired\"", "Yes"),
            ("internal_or_external", "Is your service internally or externally facing? Please select external if your service has external users.", "Options are: \"Internal\" or \"External\"", "Yes"),
            ("service_purpose", "What does your service do for users? Please avoid acronyms.", "Free text", "Yes"),
            ("service_users", "Who are the primary users of the service? ALB stands for Arms Length Body.", "Options are: \"General public\", \"Professional or licensed users\", \"Civil servants or contractors or ALBs\"", "Yes"),
            ("service_owner", "Who owns the service? ALB stands for Arms Length Body.", "Options are: \"Department\", \"ALB\" or \"External contractor\"", "Yes"),
            ("service_manager", "Who manages the service?", "Options are: \"Internally managed\" or \"External contractor\"", "Yes"),
            ("service_usage", "How often does a single user the access this service?", "Options are: \"Frequent use\", \"Daily, \"Weekly\", \"Monthly\", \"Annually/bi-annually\", \"One-time use\"", "Yes"),
            ("payment_required", "Is payment required to use the service?", "Options are: \"Yes\" or \"No\"", "Yes"),
            ("onelogin_available", "Is OneLogin available to access the service?", "Options are: \"Yes\" or \"No\"", "Yes"),
            ("online_available", "Can the service be accessed online?", "Options are: \"Yes\" or \"No\"", "Yes"),
            ("telephone_available", "Can the service be accessed by telephone?", "Options are: \"Yes\" or \"No\"", "Yes"),
            ("post_available", "Can the service be accessed by post?", "Options are: \"Yes\" or \"No\"", "Yes"),
            ("inperson_available", "Can the service be accessed in-person?", "Options are: \"Yes\" or \"No\"", "Yes"),
            ("user_satisfaction_method", "What method do you use to measure user satisfaction?", "Free text", "Yes"),
            ("digital_adoption_method", "What method do you use to measure digital adoption?", "Free text", "Yes"),
            ("digital_completion_method", "What method do you use to measure digital completion?", "Free text", "Yes"),
            ("cost_per_transaction_method", "What method do you use to measure cost per transaction (for transactional services)?", "Free text", "Yes"),
            ("service_kpi", "A service-level KPI (Key Performance Indicator), OKR (Objectives and Key Results), or MIM (Most Important Metric).", "Free text", "Yes"),
            ("kpi_value", "The service performance against the KPI (Key Performance Indicator), OKR (Objectives and Key Results), or MIM (Most Important Metric).", "Free text", "Yes"),
            ("comments", "Use this space to provide any additional comments about the data submitted. If any columns have been left empty, please explain why and include any relevant context.", "Free text", "Yes")
        };

        for (var i = 0; i < defs.Length; i++)
        {
            var row = 6 + i;
            ws.Cell(row, 1).Value = defs[i].Name;
            ws.Cell(row, 2).Value = defs[i].Description;
            ws.Cell(row, 3).Value = defs[i].Format;
            ws.Cell(row, 4).Value = defs[i].CanBeEmpty;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteServiceDefinitions(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "Dataset information for rAPId";
        ws.Cell(2, 1).Value = "Layer";
        ws.Cell(2, 2).Value = "Domain";
        ws.Cell(2, 3).Value = "Dataset";
        ws.Cell(3, 1).Value = "raw_data";
        ws.Cell(3, 2).Value = "digital_performance_dashboard_dfe";
        ws.Cell(3, 3).Value = "service_metrics_upload_performance_dashboard";

        ws.Cell(5, 1).Value = "Column Name";
        ws.Cell(5, 2).Value = "Description";
        ws.Cell(5, 3).Value = "Format";
        ws.Cell(5, 4).Value = "Can be empty";
        ws.Row(5).Style.Font.Bold = true;

        var defs = new (string Name, string Description, string Format, string CanBeEmpty)[]
        {
            ("quarter", "Financial quarter and year for the data.", "Q#_YYYY/YY. For example, Q1_2026/27", "No"),
            ("service_name", "The name of the service. This should match the published name of the service, as well as the Context Data tab.", "Free text", "No"),
            ("started_transactions", "Total number of transactions started across all channels (for example, online, phone or paper).", "Whole number", "Yes"),
            ("completed_transactions", "Total number of transactions completed across all channels (for example, online, phone or paper).", "Whole number", "Yes"),
            ("started_digital_transactions", "Total number of online transactions started.", "Whole number", "Yes"),
            ("completed_digital_transactions", "Total number of online transactions completed.", "Whole number", "Yes"),
            ("usat_total_number_of_responses", "The number of respondents who participated in a user satisfaction (USAT) survey or equivalent.", "Whole number", "Yes"),
            ("usat_number_of_satisfied_responses", "The number of respondents who selected 4 (\"Satisfied\") or equivalent in a user satisfaction (USAT) survey where 1 = \"Very dissatisfied\" and 5 = \"Very satisfied\".", "Whole number", "Yes"),
            ("usat_number_of_very_satisfied_responses", "The number of respondents who selected 5 (\"Very satisfied\") or equivalent in a user satisfaction (USAT) survey where 1 = \"Very dissatisfied\" and 5 = \"Very satisfied\".", "Whole number", "Yes"),
            ("accessibility_compliance", "Whether or not the service meets the required accessibility standard for public services (WCAG 2.2 AA or above).", "Options are: \"Yes\" or \"No\"", "Yes"),
            ("cost_per_transaction_amount", "The average cost of completing one transaction (for transactional services) in pounds (£).", "Decimal number", "Yes"),
            ("categories_included_in_cpt", "This indicates which types of costs are included when calculating the cost per transaction for transactional services.", "Free text", "Yes"),
            ("fte_count", "Number of full-time equivalent (FTE) staff working on the service.", "Decimal number", "Yes"),
            ("govuk_url", "gov.uk URL for the published service", "Free text", "Yes"),
            ("comments", "Use this space to provide any additional comments about the data submitted. If any columns have been left empty, please explain why and include any relevant context.", "Free text", "Yes")
        };

        for (var i = 0; i < defs.Length; i++)
        {
            var row = 6 + i;
            ws.Cell(row, 1).Value = defs[i].Name;
            ws.Cell(row, 2).Value = defs[i].Description;
            ws.Cell(row, 3).Value = defs[i].Format;
            ws.Cell(row, 4).Value = defs[i].CanBeEmpty;
        }

        ws.Columns().AdjustToContents();
    }
}
