using System.ComponentModel.DataAnnotations;
using FipsReporting.Data;

namespace FipsReporting.Models
{
    public class BusinessMetric
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string MeasurementType { get; set; } = string.Empty; // percentage, number, text, boolean, currency
        
        [MaxLength(100)]
        public string? Unit { get; set; } // %, users, £, etc.
        
        [MaxLength(50)]
        public string? Category { get; set; } // Performance, Financial, User Experience, etc.
        
        [MaxLength(50)]
        public string? SubCategory { get; set; } // Customer Satisfaction, Revenue, etc.
        
        public bool IsMandatory { get; set; }
        public bool AllowNotApplicable { get; set; }
        public bool IsActive { get; set; }
        
        // Business Configuration
        [MaxLength(20)]
        public string ReportingFrequency { get; set; } = "monthly"; // daily, weekly, monthly, quarterly, annually
        
        public int? TargetValue { get; set; }
        public int? MinimumValue { get; set; }
        public int? MaximumValue { get; set; }
        
        [MaxLength(20)]
        public string? TargetDirection { get; set; } // higher, lower, equal
        
        [MaxLength(1000)]
        public string? BusinessJustification { get; set; }
        
        [MaxLength(1000)]
        public string? DataSource { get; set; }
        
        [MaxLength(1000)]
        public string? CalculationMethod { get; set; }
        
        // Validation Rules
        [MaxLength(1000)]
        public string? ValidationRules { get; set; } // JSON string for complex validation
        
        public bool RequiresApproval { get; set; }
        public bool IsConfidential { get; set; }
        
        // Audit Fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        
        // Navigation Properties
        public virtual ICollection<MetricCondition> Conditions { get; set; } = new List<MetricCondition>();
        public virtual ICollection<MetricTemplate> Templates { get; set; } = new List<MetricTemplate>();
        public virtual ICollection<ReportingData> ReportingData { get; set; } = new List<ReportingData>();
    }

    public class MetricTemplate
    {
        public int Id { get; set; }
        public int BusinessMetricId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [MaxLength(20)]
        public string TemplateType { get; set; } = "standard"; // standard, quarterly, annual
        
        [MaxLength(1000)]
        public string? Instructions { get; set; }
        
        [MaxLength(1000)]
        public string? HelpText { get; set; }
        
        [MaxLength(1000)]
        public string? ExampleData { get; set; }
        
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual BusinessMetric BusinessMetric { get; set; } = null!;
    }

    public class ServiceOwner
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Department { get; set; }
        
        [MaxLength(100)]
        public string? Role { get; set; }
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        public bool IsActive { get; set; }
        public bool ReceiveNotifications { get; set; }
        public bool ReceiveReminders { get; set; }
        
        [MaxLength(20)]
        public string NotificationFrequency { get; set; } = "weekly"; // daily, weekly, monthly
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual ICollection<ProductAllocation> ProductAllocations { get; set; } = new List<ProductAllocation>();
    }

    public class ReportingWorkflow
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [MaxLength(20)]
        public string WorkflowType { get; set; } = "standard"; // standard, quarterly, annual, ad-hoc
        
        public int? BusinessMetricId { get; set; }
        
        [MaxLength(20)]
        public string Frequency { get; set; } = "monthly";
        
        public int DaysBeforeDue { get; set; } = 7; // Reminder days before due
        
        public int GracePeriodDays { get; set; } = 5; // Grace period after due date
        
        public bool RequiresApproval { get; set; }
        public bool AutoEscalate { get; set; }
        
        [MaxLength(1000)]
        public string? EscalationRules { get; set; } // JSON string
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual BusinessMetric? BusinessMetric { get; set; }
        public virtual ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    }

    public class WorkflowStep
    {
        public int Id { get; set; }
        public int ReportingWorkflowId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        public int StepOrder { get; set; }
        
        [MaxLength(50)]
        public string StepType { get; set; } = "notification"; // notification, reminder, escalation, approval
        
        [MaxLength(20)]
        public string TriggerType { get; set; } = "time_based"; // time_based, condition_based, manual
        
        public int? TriggerDays { get; set; } // Days before/after due date
        
        [MaxLength(1000)]
        public string? TriggerConditions { get; set; } // JSON string
        
        [MaxLength(1000)]
        public string? ActionTemplate { get; set; } // Email template, notification template
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual ReportingWorkflow ReportingWorkflow { get; set; } = null!;
    }

    public class DataQualityRule
    {
        public int Id { get; set; }
        public int BusinessMetricId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string RuleType { get; set; } = "validation"; // validation, range_check, format_check, business_rule
        
        [MaxLength(1000)]
        public string? RuleExpression { get; set; } // JSON or expression string
        
        [MaxLength(20)]
        public string Severity { get; set; } = "warning"; // error, warning, info
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual BusinessMetric BusinessMetric { get; set; } = null!;
    }
}
