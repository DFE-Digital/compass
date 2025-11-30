namespace Compass.Services;

/// <summary>
/// Service interface for querying accessibility training database metrics
/// </summary>
public interface IAccessibilityTrainingService
{
    /// <summary>
    /// Gets the total number of training sessions created
    /// </summary>
    Task<int> GetTotalTrainingSessionsAsync();
    
    /// <summary>
    /// Gets the total number of answers submitted across all sessions
    /// </summary>
    Task<int> GetTotalAnswersAsync();
    
    /// <summary>
    /// Gets the count of correct answers
    /// </summary>
    Task<int> GetCorrectAnswersCountAsync();
    
    /// <summary>
    /// Gets the count of incorrect answers
    /// </summary>
    Task<int> GetIncorrectAnswersCountAsync();
    
    /// <summary>
    /// Gets the count of unanswered questions
    /// </summary>
    Task<int> GetUnansweredCountAsync();
    
    /// <summary>
    /// Gets the number of sessions with all 20 questions answered (completed sessions)
    /// </summary>
    Task<int> GetCompletedSessionsCountAsync();
    
    /// <summary>
    /// Gets the completion rate as a percentage (completed sessions / total sessions)
    /// </summary>
    Task<double> GetCompletionRateAsync();
    
    /// <summary>
    /// Gets the overall correct answer rate as a percentage
    /// </summary>
    Task<double> GetCorrectAnswerRateAsync();
    
    /// <summary>
    /// Gets question-level performance statistics
    /// </summary>
    Task<List<QuestionPerformanceStats>> GetQuestionPerformanceStatsAsync();
    
    /// <summary>
    /// Gets the count of codes sent via email
    /// </summary>
    Task<int> GetCodesSentCountAsync();
    
    /// <summary>
    /// Gets training sessions created in a date range
    /// </summary>
    Task<int> GetSessionsInDateRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Gets completed training sessions grouped by month
    /// </summary>
    Task<List<MonthlySessionData>> GetCompletedSessionsByMonthAsync();
    
    /// <summary>
    /// Gets question attempt statistics (average attempts per question)
    /// </summary>
    Task<List<QuestionAttemptStats>> GetQuestionAttemptStatsAsync();
    
    /// <summary>
    /// Gets domain analysis from sent codes, excluding generic email providers
    /// </summary>
    Task<List<DomainCount>> GetDomainAnalysisAsync();
    
    /// <summary>
    /// Gets completed training sessions grouped by day for the last 30 days
    /// </summary>
    Task<List<DailySessionData>> GetCompletedSessionsByDayAsync(int days = 30);
}

/// <summary>
/// Question-level performance statistics
/// </summary>
public class QuestionPerformanceStats
{
    public int QuestionNumber { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public int TotalAnswers { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public int Unanswered { get; set; }
    public double CorrectPercentage { get; set; }
}

/// <summary>
/// Monthly session data for charts
/// </summary>
public class MonthlySessionData
{
    public string YearMonth { get; set; } = string.Empty; // Format: "YYYY-MM"
    public DateTime MonthDate { get; set; }
    public int CompletedSessions { get; set; }
    public int TotalSessions { get; set; }
}

/// <summary>
/// Question attempt statistics
/// </summary>
public class QuestionAttemptStats
{
    public int QuestionNumber { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int UniqueSessions { get; set; }
    public double AverageAttempts { get; set; }
}

/// <summary>
/// Domain count from sent codes
/// </summary>
public class DomainCount
{
    public string Domain { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Daily session data for charts
/// </summary>
public class DailySessionData
{
    public DateTime Date { get; set; }
    public string DateString { get; set; } = string.Empty; // Format: "YYYY-MM-DD"
    public int CompletedSessions { get; set; }
    public int TotalSessions { get; set; }
}

