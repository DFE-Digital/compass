namespace Compass.Models
{
    public class IssueWcagCriterion
    {
        public int Id { get; set; }
        
        public int AccessibilityIssueId { get; set; }
        public AccessibilityIssue AccessibilityIssue { get; set; } = null!;
        
        public int WcagCriterionId { get; set; }
        public WcagCriterion WcagCriterion { get; set; } = null!;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

