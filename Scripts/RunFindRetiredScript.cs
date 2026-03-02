using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Compass.Scripts;

/// <summary>
/// Console program to run the FindRetiredInCmdbButActiveInCms script.
/// 
/// Usage:
///   dotnet run --project Compass.csproj -- RunFindRetiredScript
/// 
/// Or compile and run:
///   dotnet build
///   dotnet run --no-build -- RunFindRetiredScript
/// </summary>
public class RunFindRetiredScript
{
    public static async Task Main(string[] args)
    {
        var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", $"retired-cmdb-active-cms-{DateTime.Now:yyyyMMdd-HHmmss}.log");
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
            Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .Build();

            var fipsSyncConfig = configuration.GetSection("FipsSync");
            var cmdbConfig = fipsSyncConfig.GetSection("Cmdb");
            var strapiConfig = fipsSyncConfig.GetSection("Strapi:Test"); // Using Test environment

            var cmdbEndpoint = cmdbConfig["Endpoint"] ?? throw new InvalidOperationException("CMDB Endpoint not configured");
            var cmdbUsername = cmdbConfig["Username"] ?? throw new InvalidOperationException("CMDB Username not configured");
            var cmdbPassword = cmdbConfig["Password"] ?? throw new InvalidOperationException("CMDB Password not configured");
            var cmsBaseUrl = strapiConfig["Endpoint"] ?? throw new InvalidOperationException("CMS Endpoint not configured");
            var cmsReadApiKey = strapiConfig["ApiKey"] ?? throw new InvalidOperationException("CMS ApiKey not configured");

            var finder = new FindRetiredInCmdbButActiveInCms(
                cmdbEndpoint,
                cmdbUsername,
                cmdbPassword,
                cmsBaseUrl,
                cmsReadApiKey);

            var mismatches = await finder.FindMismatchesAsync();
            finder.PrintResults(mismatches);

            Console.WriteLine();
            Console.WriteLine($"Completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            finder.Dispose();

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
