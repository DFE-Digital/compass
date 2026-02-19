using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Compass.Scripts;

/// <summary>
/// Console program to run the UpdateRetiredProductsToDecommissioned script.
/// 
/// Usage:
///   dotnet run -- --update-retired-products
/// </summary>
public class RunUpdateRetiredProducts
{
    public static async Task Main(string[] args)
    {
        var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", $"update-retired-products-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        var rollbackFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", $"rollback-retired-products-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        var logDirectory = Path.GetDirectoryName(logFilePath);
        
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        // Create a string writer to capture all output
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            // Create a writer that writes to both console and string builder
            using var logWriter = new StringWriter(output);
            using var multiWriter = new MultiTextWriter(originalOut, logWriter);
            Console.SetOut(multiWriter);

            Console.WriteLine($"Log file: {logFilePath}");
            Console.WriteLine($"Rollback file: {rollbackFilePath}");
            Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .Build();

            var fipsSyncConfig = configuration.GetSection("FipsSync");
            var strapiConfig = fipsSyncConfig.GetSection("Strapi:Test"); // Using Test environment

            var cmsBaseUrl = strapiConfig["Endpoint"] ?? throw new InvalidOperationException("CMS Endpoint not configured");
            var cmsWriteApiKey = strapiConfig["ApiKey"] ?? throw new InvalidOperationException("CMS ApiKey not configured");

            // Load the products to update from the log file
            Console.WriteLine("Loading products to update from log file...");
            var productsToUpdate = LoadProductsFromLogFile();
            Console.WriteLine($"Loaded {productsToUpdate.Count} products to update\n");

            if (productsToUpdate.Count == 0)
            {
                Console.WriteLine("No products found to update. Exiting.");
                return;
            }

            // Confirm before proceeding
            Console.WriteLine($"WARNING: This will update {productsToUpdate.Count} products to set Phase = 'Decommissioned'");
            Console.WriteLine("Press Enter to continue or Ctrl+C to cancel...");
            Console.ReadLine();

            var updater = new UpdateRetiredProductsToDecommissioned(
                cmsBaseUrl,
                cmsWriteApiKey);

            var summary = await updater.UpdateProductsAsync(productsToUpdate);
            updater.PrintResults(summary);

            // Save rollback log
            var rollbackLog = updater.GetRollbackLog();
            var rollbackJson = JsonSerializer.Serialize(rollbackLog, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(rollbackFilePath, rollbackJson);
            Console.WriteLine($"Rollback log saved to: {rollbackFilePath}");

            Console.WriteLine();
            Console.WriteLine($"Completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            updater.Dispose();

            // Write to log file
            await File.WriteAllTextAsync(logFilePath, output.ToString());
            Console.WriteLine($"Results saved to: {logFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Ensure error is also written to log
            output.AppendLine($"Error: {ex.Message}");
            output.AppendLine($"Stack trace: {ex.StackTrace}");
            await File.WriteAllTextAsync(logFilePath, output.ToString());
            
            Environment.Exit(1);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static List<MismatchEntry> LoadProductsFromLogFile()
    {
        var products = new List<MismatchEntry>();
        
        // Find the most recent log file
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        if (!Directory.Exists(logDirectory))
        {
            return products;
        }

        var logFiles = Directory.GetFiles(logDirectory, "retired-cmdb-active-cms-*.log")
            .OrderByDescending(f => File.GetCreationTime(f))
            .ToList();

        if (!logFiles.Any())
        {
            Console.WriteLine("No log file found. Please run the find script first.");
            return products;
        }

        var latestLogFile = logFiles.First();
        Console.WriteLine($"Reading from: {latestLogFile}");

        var logContent = File.ReadAllText(latestLogFile);
        
        // Parse JSON section from the log file
        var jsonStart = logContent.IndexOf("=== JSON Output ===");
        if (jsonStart == -1)
        {
            Console.WriteLine("Could not find JSON section in log file.");
            return products;
        }

        var jsonContentStart = jsonStart + "=== JSON Output ===".Length;
        var jsonContent = logContent.Substring(jsonContentStart).Trim();
        
        // Find the end of the JSON array - look for the closing bracket followed by newline or end of file
        // The JSON array should end with ] followed by optional whitespace
        var jsonArrayEnd = jsonContent.IndexOf(']');
        if (jsonArrayEnd == -1)
        {
            Console.WriteLine("Could not find end of JSON array in log file.");
            return products;
        }
        
        // Extract just the JSON array (including the closing bracket)
        var jsonArray = jsonContent.Substring(0, jsonArrayEnd + 1).Trim();
        
        try
        {
            products = JsonSerializer.Deserialize<List<MismatchEntry>>(jsonArray, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<MismatchEntry>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing JSON from log file: {ex.Message}");
            Console.WriteLine($"JSON content length: {jsonArray.Length}");
            Console.WriteLine($"First 200 chars: {jsonArray.Substring(0, Math.Min(200, jsonArray.Length))}");
            Console.WriteLine($"Last 200 chars: {jsonArray.Substring(Math.Max(0, jsonArray.Length - 200))}");
        }

        return products;
    }

    private class MultiTextWriter : TextWriter
    {
        private readonly TextWriter[] _writers;

        public MultiTextWriter(params TextWriter[] writers)
        {
            _writers = writers;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            foreach (var writer in _writers)
            {
                writer.Write(value);
            }
        }

        public override void Write(string? value)
        {
            foreach (var writer in _writers)
            {
                writer.Write(value);
            }
        }

        public override void WriteLine(string? value)
        {
            foreach (var writer in _writers)
            {
                writer.WriteLine(value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var writer in _writers)
                {
                    if (writer != Console.Out)
                    {
                        writer.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
