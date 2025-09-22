using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FipsReporting.Data
{
    public class ReportingDbContext : DbContext
    {
        public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options)
        {
        }

        public DbSet<ReportingMetric> ReportingMetrics { get; set; }
        public DbSet<MetricCondition> MetricConditions { get; set; }
        public DbSet<ProductAllocation> ProductAllocations { get; set; }
        public DbSet<ReportingData> ReportingData { get; set; }
        public DbSet<Milestone> Milestones { get; set; }
        public DbSet<MilestoneUpdate> MilestoneUpdates { get; set; }
        public DbSet<Objective> Objectives { get; set; }
        public DbSet<ReportingUser> ReportingUsers { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
        public DbSet<PerformanceMetricData> PerformanceMetricData { get; set; }
        
        // Business Intelligence Models (commented out temporarily for port testing)
        // public DbSet<BusinessMetric> BusinessMetrics { get; set; }
        // public DbSet<MetricTemplate> MetricTemplates { get; set; }
        // public DbSet<ServiceOwner> ServiceOwners { get; set; }
        // public DbSet<ReportingWorkflow> ReportingWorkflows { get; set; }
        // public DbSet<WorkflowStep> WorkflowSteps { get; set; }
        // public DbSet<DataQualityRule> DataQualityRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ReportingMetric
            modelBuilder.Entity<ReportingMetric>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.MeasurementType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IsMandatory).HasDefaultValue(false);
                entity.Property(e => e.AllowNotApplicable).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure MetricCondition
            modelBuilder.Entity<MetricCondition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CategoryType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CategoryValue).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Operator).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.ReportingMetric)
                    .WithMany(e => e.Conditions)
                    .HasForeignKey(e => e.ReportingMetricId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProductAllocation
            modelBuilder.Entity<ProductAllocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AllocatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.ProductId, e.UserEmail }).IsUnique();
            });

            // Configure ReportingData
            modelBuilder.Entity<ReportingData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).HasMaxLength(50);
                entity.Property(e => e.MetricId).IsRequired();
                entity.Property(e => e.Value).HasMaxLength(1000);
                entity.Property(e => e.ReportingPeriod).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Comment).HasMaxLength(2000);
                entity.Property(e => e.SubmittedBy).IsRequired().HasMaxLength(255);
                entity.Property(e => e.SubmittedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Metric)
                    .WithMany()
                    .HasForeignKey(e => e.MetricId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Milestone
            modelBuilder.Entity<Milestone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FipsId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Priority).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastUpdatedBy).HasMaxLength(255);
                
                // Configure additional properties
                entity.Property(e => e.ProductId).HasMaxLength(50);
                entity.Property(e => e.RagStatus).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Foreign key relationship to Objective
                entity.HasOne(e => e.Objective)
                    .WithMany(e => e.Milestones)
                    .HasForeignKey(e => e.ObjectiveId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure MilestoneUpdate
            modelBuilder.Entity<MilestoneUpdate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UpdateText).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.StatusChange).HasMaxLength(50);
                entity.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(255);
                entity.Property(e => e.UpdateDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Configure additional properties
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.RagStatus).HasMaxLength(20);
                entity.Property(e => e.Comment).HasMaxLength(2000);
                
                entity.HasOne(e => e.Milestone)
                    .WithMany(e => e.Updates)
                    .HasForeignKey(e => e.MilestoneId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Objective
            modelBuilder.Entity<Objective>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                
                // Create unique index on Reference
                entity.HasIndex(e => e.Reference).IsUnique();
            });

            // Configure ReportingUser
            modelBuilder.Entity<ReportingUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure UserPermission
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                
                // Create unique index on email
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure PerformanceMetric
            modelBuilder.Entity<PerformanceMetric>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Notice).HasMaxLength(2000);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Measure).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Mandate).HasMaxLength(50);
                entity.Property(e => e.ValidationCriteria).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                
                // Create unique index on UniqueId
                entity.HasIndex(e => e.UniqueId).IsUnique();
            });

            // Configure PerformanceMetricData
            modelBuilder.Entity<PerformanceMetricData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ReportingPeriod).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Value).HasMaxLength(1000);
                entity.Property(e => e.Comment).HasMaxLength(2000);
                entity.Property(e => e.SubmittedBy).IsRequired().HasMaxLength(255);
                entity.Property(e => e.SubmittedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.PerformanceMetric)
                    .WithMany(e => e.PerformanceData)
                    .HasForeignKey(e => e.PerformanceMetricId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public class ReportingMetric
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Required]
        [MaxLength(50)]
        public string MeasurementType { get; set; } = string.Empty; // percentage, number, text
        public bool IsMandatory { get; set; }
        public bool AllowNotApplicable { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<MetricCondition> Conditions { get; set; } = new List<MetricCondition>();
        public virtual ICollection<ReportingData> ReportingData { get; set; } = new List<ReportingData>();
    }

    public class MetricCondition
    {
        public int Id { get; set; }
        public int ReportingMetricId { get; set; }
        [Required]
        [MaxLength(100)]
        public string CategoryType { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string CategoryValue { get; set; } = string.Empty;
        [Required]
        [MaxLength(20)]
        public string Operator { get; set; } = string.Empty; // contains, equals, not_contains

        // Navigation properties
        public virtual ReportingMetric ReportingMetric { get; set; } = null!;
    }

    public class ProductAllocation
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string ProductId { get; set; } = string.Empty;
        [Required]
        [MaxLength(255)]
        public string UserEmail { get; set; } = string.Empty;
        public DateTime AllocatedAt { get; set; }
        public string? AllocatedBy { get; set; }
    }

    public class ReportingData
    {
        public int Id { get; set; }
        public int MetricId { get; set; }
        [MaxLength(50)]
        public string? ProductId { get; set; }
        [MaxLength(1000)]
        public string Value { get; set; } = string.Empty;
        [Required]
        [MaxLength(20)]
        public string ReportingPeriod { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Comment { get; set; }
        [Required]
        [MaxLength(255)]
        public string SubmittedBy { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ReportingMetric? Metric { get; set; }
    }

    public class Milestone
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string FipsId { get; set; } = string.Empty;
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Not Started, In Progress, Completed, Overdue, Cancelled
        public DateTime? TargetDate { get; set; }
        public DateTime? ActualDate { get; set; }
        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = string.Empty; // High, Medium, Low
        [Required]
        [MaxLength(255)]
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        [MaxLength(255)]
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public int? ObjectiveId { get; set; }

        // Additional properties for compatibility with existing code
        public DateTime? DueDate { get; set; } // Alias for TargetDate
        public string? ProductId { get; set; } // Alias for FipsId
        public string? RagStatus { get; set; } // Calculated RAG status
        public DateTime CreatedAt { get; set; } // Alias for CreatedDate
        public DateTime? UpdatedAt { get; set; } // Alias for LastUpdatedDate
        public string? ProductName { get; set; } // Product name for display purposes

        // Navigation properties
        public virtual Objective? Objective { get; set; }
        public virtual ICollection<MilestoneUpdate> Updates { get; set; } = new List<MilestoneUpdate>();
    }

    public class MilestoneUpdate
    {
        public int Id { get; set; }
        public int MilestoneId { get; set; }
        public DateTime UpdateDate { get; set; }
        [Required]
        [MaxLength(2000)]
        public string UpdateText { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? StatusChange { get; set; }
        [Required]
        [MaxLength(255)]
        public string UpdatedBy { get; set; } = string.Empty;

        // Additional properties for compatibility with existing code
        public DateTime? UpdatedAt { get; set; } // Alias for UpdateDate
        public string? Status { get; set; } // Status change from this update
        public string? RagStatus { get; set; } // RAG status at time of update
        public string? Comment { get; set; } // Additional comment
        public DateTime? NewDueDate { get; set; } // New due date if changed

        // Navigation properties
        public virtual Milestone Milestone { get; set; } = null!;
    }

    public class ReportingUser
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty; // reporting_user, admin, central_operations
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class PerformanceMetric
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string UniqueId { get; set; } = string.Empty; // Unique identifier
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }
        public bool LegalRegulatory { get; set; } // Legal/Regulatory flag
        [MaxLength(50)]
        public string Mandate { get; set; } = string.Empty; // Legal, DSIT, DfE, DDT
        [MaxLength(500)]
        public string? ApplicablePhases { get; set; } // JSON string of applicable phase names from CMS
        [MaxLength(2000)]
        public string? Notice { get; set; }
        public string ReportableInPhase { get; set; } = string.Empty; // JSON string of phase IDs from CMS
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // Category for grouping metrics
        [Required]
        [MaxLength(50)]
        public string Measure { get; set; } = string.Empty; // number, decimal, options_list, boolean
        public bool Mandatory { get; set; }
        [MaxLength(2000)]
        public string? ValidationCriteria { get; set; } // JSON string with validation rules
        public bool CanReportNullReturn { get; set; } // Can the metric be reported as null?
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<PerformanceMetricData> PerformanceData { get; set; } = new List<PerformanceMetricData>();
    }

    public class PerformanceMetricData
    {
        public int Id { get; set; }
        public int PerformanceMetricId { get; set; }
        [MaxLength(50)]
        public string ProductId { get; set; } = string.Empty; // FIPS ID
        [MaxLength(20)]
        public string ReportingPeriod { get; set; } = string.Empty; // e.g., "2025-08"
        [MaxLength(1000)]
        public string? Value { get; set; } // The reported value
        [MaxLength(2000)]
        public string? Comment { get; set; }
        public bool IsNullReturn { get; set; } // Whether this is a null return
        public bool IsSubmitted { get; set; } // Whether the entire report has been submitted
        [Required]
        [MaxLength(255)]
        public string SubmittedBy { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual PerformanceMetric PerformanceMetric { get; set; } = null!;
    }

    public class Objective
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string Reference { get; set; } = string.Empty; // Unique reference code
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Active, Completed, Cancelled, On Hold
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // DDT Objective, Government Mission, Flagship, Other
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        [MaxLength(255)]
        public string? CreatedBy { get; set; }
        [MaxLength(255)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    }
}
