using ClosedXML.Excel;
using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

public class ServiceCensusDataExcelExportTests
{
    [Fact]
    public void SanitizeSheetName_ReplacesInvalidCharsAndTruncates()
    {
        var longName = new string('A', 40);
        Assert.Equal(31, ServiceCensusDataExcelExport.SanitizeSheetName(longName).Length);
        Assert.Equal("Theme-name", ServiceCensusDataExcelExport.SanitizeSheetName("Theme:name"));
        Assert.Equal("A-B-C", ServiceCensusDataExcelExport.SanitizeSheetName("A/B*C"));
    }

    [Fact]
    public void UniqueSheetName_DeduplicatesWithin31Chars()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal("Users", ServiceCensusDataExcelExport.UniqueSheetName("Users", used));
        Assert.Equal("Users (2)", ServiceCensusDataExcelExport.UniqueSheetName("Users", used));
        Assert.Equal("Users (3)", ServiceCensusDataExcelExport.UniqueSheetName("Users", used));
    }

    [Fact]
    public void FormatAnswerForExport_ResolvesLookupsAndYesNo()
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cap-1"] = "Risk (GRC27372)",
            ["ug-9"] = "Schools"
        };

        var yes = new ServiceCensusAnswerFact
        {
            FieldType = ServiceDataModelFieldType.YesNo,
            HasAnswer = true,
            ChoiceKeys = new[] { "Yes" }
        };
        Assert.Equal("Yes", ServiceCensusAnalysisAggregation.FormatAnswerForExport(yes, labels));

        var caps = new ServiceCensusAnswerFact
        {
            FieldType = ServiceDataModelFieldType.Lookup,
            AllowMultiple = true,
            HasAnswer = true,
            ChoiceKeys = new[] { "cap-1", "ug-9" }
        };
        Assert.Equal("Risk (GRC27372), Schools",
            ServiceCensusAnalysisAggregation.FormatAnswerForExport(caps, labels));

        var blank = new ServiceCensusAnswerFact
        {
            FieldType = ServiceDataModelFieldType.Text,
            HasAnswer = false
        };
        Assert.Equal("", ServiceCensusAnalysisAggregation.FormatAnswerForExport(blank, labels));
    }

    [Fact]
    public void BuildWorkbook_CreatesOneSheetPerTheme_WithHeadersWhenEmpty()
    {
        var model = new ServiceCensusDataExportModel
        {
            Themes = new[]
            {
                new ServiceCensusThemeDefinition
                {
                    StableKey = "users",
                    Name = "Users",
                    SortOrder = 1,
                    Fields = new[]
                    {
                        new ServiceCensusFieldDefinition
                        {
                            StableKey = "primary-user-group-type",
                            Label = "Primary user group type",
                            SortOrder = 1
                        }
                    }
                },
                new ServiceCensusThemeDefinition
                {
                    StableKey = "tech",
                    Name = "Technology",
                    SortOrder = 2,
                    Fields = new[]
                    {
                        new ServiceCensusFieldDefinition
                        {
                            StableKey = "stack",
                            Label = "Stack",
                            SortOrder = 1
                        }
                    }
                }
            },
            Services = Array.Empty<ServiceCensusDataExportServiceRow>()
        };

        var bytes = ServiceCensusDataExcelExport.BuildWorkbook(model);
        Assert.True(bytes.Length > 0);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.Equal("Users", workbook.Worksheet(1).Name);
        Assert.Equal("Technology", workbook.Worksheet(2).Name);
        Assert.Equal("Service name", workbook.Worksheet(1).Cell(1, 1).GetString());
        Assert.Equal("Register ID", workbook.Worksheet(1).Cell(1, 2).GetString());
        Assert.Equal("Product ID", workbook.Worksheet(1).Cell(1, 3).GetString());
        Assert.Equal("Primary user group type", workbook.Worksheet(1).Cell(1, 4).GetString());
        Assert.True(workbook.Worksheet(1).LastRowUsed()!.RowNumber() == 1);
    }

    [Fact]
    public void BuildWorkbook_WritesServiceThenAnswers()
    {
        var productId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var model = new ServiceCensusDataExportModel
        {
            Themes = new[]
            {
                new ServiceCensusThemeDefinition
                {
                    StableKey = "users",
                    Name = "Users",
                    SortOrder = 1,
                    Fields = new[]
                    {
                        new ServiceCensusFieldDefinition
                        {
                            StableKey = "q1",
                            Label = "Question one",
                            SortOrder = 1
                        }
                    }
                }
            },
            Services = new[]
            {
                new ServiceCensusDataExportServiceRow
                {
                    ProductId = productId,
                    RegisterId = 42,
                    ServiceName = "Example service",
                    AnswersByFieldStableKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["q1"] = "Schools"
                    }
                }
            }
        };

        var bytes = ServiceCensusDataExcelExport.BuildWorkbook(model);
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        Assert.Equal("Example service", sheet.Cell(2, 1).GetString());
        Assert.Equal(42, sheet.Cell(2, 2).GetValue<int>());
        Assert.Equal(productId.ToString(), sheet.Cell(2, 3).GetString());
        Assert.Equal("Schools", sheet.Cell(2, 4).GetString());
    }
}
