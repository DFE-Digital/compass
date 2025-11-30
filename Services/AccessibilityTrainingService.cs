using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Compass.Services;

/// <summary>
/// Service for querying accessibility training database metrics
/// </summary>
public class AccessibilityTrainingService : IAccessibilityTrainingService
{
    private readonly string _connectionString;
    private readonly ILogger<AccessibilityTrainingService> _logger;

    public AccessibilityTrainingService(
        IConfiguration configuration,
        ILogger<AccessibilityTrainingService> logger)
    {
        var rawConnectionString = configuration.GetConnectionString("AccessibilityTraining") 
            ?? throw new InvalidOperationException("AccessibilityTraining connection string not found in configuration.");
        
        // Convert postgres:// URI format to standard Npgsql connection string format
        _connectionString = ConvertPostgresUriToConnectionString(rawConnectionString);
        _logger = logger;
    }

    /// <summary>
    /// Converts a postgres:// URI format connection string to Npgsql standard format
    /// </summary>
    private static string ConvertPostgresUriToConnectionString(string uri)
    {
        // If it's already in standard format, return as-is
        if (!uri.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) && 
            !uri.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        try
        {
            // Remove the protocol prefix
            var withoutProtocol = uri.Contains("://") ? uri.Substring(uri.IndexOf("://") + 3) : uri;
            
            // Find the @ symbol that separates credentials from host
            var atIndex = withoutProtocol.LastIndexOf('@');
            if (atIndex == -1)
            {
                throw new ArgumentException("Invalid postgres:// URI format: missing @ separator", nameof(uri));
            }
            
            // Split credentials and host parts
            var credentialsPart = withoutProtocol.Substring(0, atIndex);
            var hostPart = withoutProtocol.Substring(atIndex + 1);
            
            // Parse credentials (username:password)
            var colonIndex = credentialsPart.IndexOf(':');
            var username = colonIndex > 0 
                ? Uri.UnescapeDataString(credentialsPart.Substring(0, colonIndex))
                : Uri.UnescapeDataString(credentialsPart);
            var password = colonIndex > 0 && colonIndex < credentialsPart.Length - 1
                ? Uri.UnescapeDataString(credentialsPart.Substring(colonIndex + 1))
                : string.Empty;
            
            // Parse host part (host:port/database)
            var pathSlashIndex = hostPart.IndexOf('/');
            var hostPortPart = pathSlashIndex > 0 ? hostPart.Substring(0, pathSlashIndex) : hostPart;
            var database = pathSlashIndex > 0 && pathSlashIndex < hostPart.Length - 1
                ? Uri.UnescapeDataString(hostPart.Substring(pathSlashIndex + 1))
                : string.Empty;
            
            // Parse host and port
            var portColonIndex = hostPortPart.LastIndexOf(':');
            var host = portColonIndex > 0 
                ? hostPortPart.Substring(0, portColonIndex)
                : hostPortPart;
            var port = portColonIndex > 0 && portColonIndex < hostPortPart.Length - 1
                ? int.Parse(hostPortPart.Substring(portColonIndex + 1))
                : 5432; // Default PostgreSQL port

            // Build standard Npgsql connection string
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = SslMode.Require,
                TrustServerCertificate = true // Required for AWS RDS
            };

            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to parse postgres:// URI: {ex.Message}", nameof(uri), ex);
        }
    }

    public async Task<int> GetTotalTrainingSessionsAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM accessibility_manual.training_sessions",
                connection);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total training sessions");
            return 0;
        }
    }

    public async Task<int> GetTotalAnswersAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM accessibility_manual.answers",
                connection);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total answers");
            return 0;
        }
    }

    public async Task<int> GetCorrectAnswersCountAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM accessibility_manual.answers WHERE answer_status = 'Correct'",
                connection);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting correct answers count");
            return 0;
        }
    }

    public async Task<int> GetIncorrectAnswersCountAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM accessibility_manual.answers WHERE answer_status = 'Incorrect'",
                connection);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting incorrect answers count");
            return 0;
        }
    }

    public async Task<int> GetUnansweredCountAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM accessibility_manual.answers WHERE answer_status = 'Not answered'",
                connection);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unanswered count");
            return 0;
        }
    }

    public async Task<int> GetCompletedSessionsCountAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // A session is completed if it has exactly 20 answers
            await using var command = new NpgsqlCommand(
                @"SELECT COUNT(*) 
                  FROM (
                      SELECT training_session_id, COUNT(*) as answer_count
                      FROM accessibility_manual.answers
                      GROUP BY training_session_id
                      HAVING COUNT(*) = 20
                  ) completed_sessions",
                connection);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completed sessions count");
            return 0;
        }
    }

    public async Task<double> GetCompletionRateAsync()
    {
        try
        {
            var totalSessions = await GetTotalTrainingSessionsAsync();
            if (totalSessions == 0) return 0;
            
            var completedSessions = await GetCompletedSessionsCountAsync();
            return Math.Round((double)completedSessions / totalSessions * 100, 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating completion rate");
            return 0;
        }
    }

    public async Task<double> GetCorrectAnswerRateAsync()
    {
        try
        {
            var totalAnswers = await GetTotalAnswersAsync();
            if (totalAnswers == 0) return 0;
            
            var correctAnswers = await GetCorrectAnswersCountAsync();
            return Math.Round((double)correctAnswers / totalAnswers * 100, 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating correct answer rate");
            return 0;
        }
    }

    public async Task<List<QuestionPerformanceStats>> GetQuestionPerformanceStatsAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(
                @"SELECT 
                    question_number,
                    question_type,
                    COUNT(*) as total_answers,
                    COUNT(CASE WHEN answer_status = 'Correct' THEN 1 END) as correct_answers,
                    COUNT(CASE WHEN answer_status = 'Incorrect' THEN 1 END) as incorrect_answers,
                    COUNT(CASE WHEN answer_status = 'Not answered' THEN 1 END) as unanswered
                  FROM accessibility_manual.answers
                  GROUP BY question_number, question_type
                  ORDER BY question_number",
                connection);
            
            var stats = new List<QuestionPerformanceStats>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var total = reader.GetInt32(2);
                var correct = reader.GetInt32(3);
                var correctPercentage = total > 0 ? Math.Round((double)correct / total * 100, 1) : 0;
                
                stats.Add(new QuestionPerformanceStats
                {
                    QuestionNumber = reader.GetInt32(0),
                    QuestionType = reader.GetString(1),
                    TotalAnswers = total,
                    CorrectAnswers = correct,
                    IncorrectAnswers = reader.GetInt32(4),
                    Unanswered = reader.GetInt32(5),
                    CorrectPercentage = correctPercentage
                });
            }
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question performance stats");
            return new List<QuestionPerformanceStats>();
        }
    }

    public async Task<int> GetCodesSentCountAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM accessibility_manual.sent_codes",
                connection);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting codes sent count");
            return 0;
        }
    }

    public async Task<int> GetSessionsInDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM accessibility_manual.training_sessions WHERE created_at >= @startDate AND created_at <= @endDate",
                connection);
            
            command.Parameters.AddWithValue("startDate", startDate);
            command.Parameters.AddWithValue("endDate", endDate);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions in date range");
            return 0;
        }
    }

    public async Task<List<MonthlySessionData>> GetCompletedSessionsByMonthAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // Get completed sessions (with 20 answers) grouped by month
            await using var command = new NpgsqlCommand(
                @"SELECT 
                    TO_CHAR(ts.created_at, 'YYYY-MM') as year_month,
                    DATE_TRUNC('month', ts.created_at) as month_date,
                    COUNT(DISTINCT CASE WHEN answer_counts.answer_count = 20 THEN ts.id END) as completed_sessions,
                    COUNT(DISTINCT ts.id) as total_sessions
                  FROM accessibility_manual.training_sessions ts
                  LEFT JOIN (
                      SELECT training_session_id, COUNT(*) as answer_count
                      FROM accessibility_manual.answers
                      GROUP BY training_session_id
                  ) answer_counts ON ts.id = answer_counts.training_session_id
                  GROUP BY TO_CHAR(ts.created_at, 'YYYY-MM'), DATE_TRUNC('month', ts.created_at)
                  ORDER BY month_date",
                connection);
            
            var monthlyData = new List<MonthlySessionData>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                monthlyData.Add(new MonthlySessionData
                {
                    YearMonth = reader.GetString(0),
                    MonthDate = reader.GetDateTime(1),
                    CompletedSessions = reader.GetInt32(2),
                    TotalSessions = reader.GetInt32(3)
                });
            }
            
            return monthlyData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completed sessions by month");
            return new List<MonthlySessionData>();
        }
    }

    public async Task<List<QuestionAttemptStats>> GetQuestionAttemptStatsAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // Calculate attempts per question
            // An attempt is counted each time an answer is updated (could be multiple attempts per session)
            // But for simplicity, we'll count unique sessions that answered the question
            await using var command = new NpgsqlCommand(
                @"SELECT 
                    question_number,
                    MAX(question_type) as question_type,
                    COUNT(*) as total_attempts,
                    COUNT(DISTINCT training_session_id) as unique_sessions,
                    CASE 
                        WHEN COUNT(DISTINCT training_session_id) > 0 
                        THEN ROUND(COUNT(*)::numeric / COUNT(DISTINCT training_session_id), 2)
                        ELSE 0 
                    END as average_attempts
                  FROM accessibility_manual.answers
                  WHERE answer_status != 'Not answered'
                  GROUP BY question_number
                  ORDER BY question_number",
                connection);
            
            var stats = new List<QuestionAttemptStats>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                stats.Add(new QuestionAttemptStats
                {
                    QuestionNumber = reader.GetInt32(0),
                    QuestionType = reader.GetString(1),
                    TotalAttempts = reader.GetInt32(2),
                    UniqueSessions = reader.GetInt32(3),
                    AverageAttempts = reader.GetDouble(4)
                });
            }
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question attempt stats");
            return new List<QuestionAttemptStats>();
        }
    }

    public async Task<List<DomainCount>> GetDomainAnalysisAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // Extract domain from email and count, excluding generic email providers
            // Using SPLIT_PART to extract domain after @ symbol
            await using var command = new NpgsqlCommand(
                @"SELECT 
                    LOWER(SPLIT_PART(email, '@', 2)) as domain,
                    COUNT(*) as count
                  FROM accessibility_manual.sent_codes
                  WHERE email LIKE '%@%'
                  AND LOWER(SPLIT_PART(email, '@', 2)) NOT IN (
                      'gmail.com', 'googlemail.com', 'live.com', 'icloud.com',
                      'outlook.com', 'hotmail.com', 'yahoo.com', 'ymail.com',
                      'me.com', 'mac.com', 'msn.com', 'aol.com'
                  )
                  GROUP BY LOWER(SPLIT_PART(email, '@', 2))
                  HAVING COUNT(*) > 0
                  ORDER BY count DESC",
                connection);
            
            var domains = new List<DomainCount>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var domain = reader.IsDBNull(0) ? "unknown" : reader.GetString(0);
                // Skip if domain is empty or null
                if (string.IsNullOrWhiteSpace(domain) || domain == "unknown")
                    continue;
                    
                domains.Add(new DomainCount
                {
                    Domain = domain,
                    Count = reader.GetInt32(1)
                });
            }
            
            return domains;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting domain analysis");
            return new List<DomainCount>();
        }
    }

    public async Task<List<DailySessionData>> GetCompletedSessionsByDayAsync(int days = 30)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var startDate = DateTime.UtcNow.AddDays(-days).Date;
            
            // Get completed sessions (with 20 answers) grouped by day based on session creation date
            await using var command = new NpgsqlCommand(
                @"SELECT 
                    DATE(ts.created_at) as session_date,
                    COUNT(DISTINCT CASE WHEN answer_counts.answer_count = 20 THEN ts.id END) as completed_sessions,
                    COUNT(DISTINCT ts.id) as total_sessions
                  FROM accessibility_manual.training_sessions ts
                  LEFT JOIN (
                      SELECT training_session_id, COUNT(*) as answer_count
                      FROM accessibility_manual.answers
                      GROUP BY training_session_id
                  ) answer_counts ON ts.id = answer_counts.training_session_id
                  WHERE DATE(ts.created_at) >= @startDate
                  GROUP BY DATE(ts.created_at)
                  ORDER BY session_date",
                connection);
            
            command.Parameters.AddWithValue("startDate", startDate);
            
            var dailyData = new List<DailySessionData>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var date = reader.GetDateTime(0);
                dailyData.Add(new DailySessionData
                {
                    Date = date,
                    DateString = date.ToString("yyyy-MM-dd"),
                    CompletedSessions = reader.GetInt32(1),
                    TotalSessions = reader.GetInt32(2)
                });
            }
            
            // Fill in missing days with zero values
            var allDays = new List<DailySessionData>();
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var existingData = dailyData.FirstOrDefault(d => d.Date.Date == date.Date);
                
                allDays.Add(existingData ?? new DailySessionData
                {
                    Date = date,
                    DateString = date.ToString("yyyy-MM-dd"),
                    CompletedSessions = 0,
                    TotalSessions = 0
                });
            }
            
            return allDays;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completed sessions by day");
            return new List<DailySessionData>();
        }
    }
}

