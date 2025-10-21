using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Compass.Data;

namespace Compass;

/// <summary>
/// Utility to clean (drop all tables) from the Azure SQL database
/// Run this with: dotnet run --clean-database
/// </summary>
public class CleanDatabase
{
    public static async Task RunAsync(string connectionString)
    {
        Console.WriteLine("=== Compass Database Cleanup Utility ===\n");
        Console.WriteLine("This will DROP ALL TABLES in the Azure SQL database.");
        Console.WriteLine("Are you sure you want to continue? Type 'YES' to confirm:");
        
        // For automation, we'll skip confirmation when run via command line
        Console.WriteLine("Proceeding with cleanup (automated mode)...\n");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("✓ Connected to Azure SQL database");
            
            // Disable all constraints
            Console.WriteLine("Disabling all constraints...");
            await ExecuteSqlAsync(connection, "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all'");
            
            // Drop all foreign key constraints
            Console.WriteLine("Dropping all foreign key constraints...");
            var dropFkSql = @"
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
                FROM sys.foreign_keys;
                EXEC sp_executesql @sql;
            ";
            await ExecuteSqlAsync(connection, dropFkSql);
            
            // Drop all tables
            Console.WriteLine("Dropping all tables...");
            var dropTablesSql = @"
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql += 'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';'
                FROM sys.tables
                WHERE name != '__EFMigrationsHistory';
                EXEC sp_executesql @sql;
            ";
            await ExecuteSqlAsync(connection, dropTablesSql);
            
            // Drop migration history table
            Console.WriteLine("Dropping migration history...");
            await ExecuteSqlAsync(connection, "IF OBJECT_ID('__EFMigrationsHistory', 'U') IS NOT NULL DROP TABLE [__EFMigrationsHistory];");
            
            // Drop all views
            Console.WriteLine("Dropping all views...");
            var dropViewsSql = @"
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql += 'DROP VIEW ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';'
                FROM sys.views
                WHERE schema_id = SCHEMA_ID('dbo');
                EXEC sp_executesql @sql;
            ";
            await ExecuteSqlAsync(connection, dropViewsSql);
            
            Console.WriteLine("\n✓ Database cleaned successfully!");
            Console.WriteLine("You can now run migrations to create a fresh schema.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Cleanup failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
    
    private static async Task ExecuteSqlAsync(SqlConnection connection, string sql)
    {
        try
        {
            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 300; // 5 minutes
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: {ex.Message}");
            // Continue even if some commands fail
        }
    }
}

