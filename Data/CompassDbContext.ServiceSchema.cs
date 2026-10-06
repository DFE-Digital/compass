using Compass.Models;
using Compass.Models.ServiceSchema;
using Microsoft.EntityFrameworkCore;

namespace Compass.Data;

public partial class CompassDbContext
{
    public DbSet<StaffRole> StaffRoles { get; set; } = null!;
    public DbSet<ServiceSchemaLookupSet> ServiceSchemaLookupSets { get; set; } = null!;
    public DbSet<ServiceSchemaLookupValue> ServiceSchemaLookupValues { get; set; } = null!;
    public DbSet<CensusServiceLine> CensusServiceLines { get; set; } = null!;
    public DbSet<CensusService> CensusServices { get; set; } = null!;
    public DbSet<CensusServiceLineService> CensusServiceLineServices { get; set; } = null!;
    public DbSet<CensusServiceProduct> CensusServiceProducts { get; set; } = null!;
    public DbSet<CensusCatalogueItem> CensusCatalogueItems { get; set; } = null!;
    public DbSet<CensusEntry> CensusEntries { get; set; } = null!;
    public DbSet<CensusSectionDeclaration> CensusSectionDeclarations { get; set; } = null!;
    public DbSet<ServiceSchemaAreaConfig> ServiceSchemaAreaConfigs { get; set; } = null!;
    public DbSet<ServiceSchemaQuestion> ServiceSchemaQuestions { get; set; } = null!;

    private void ConfigureServiceSchema(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StaffRole>(e =>
        {
            e.HasIndex(r => r.Code).IsUnique();
            e.Property(r => r.Family).HasMaxLength(80);
            e.Property(r => r.Name).HasMaxLength(100);
            e.Property(r => r.Code).HasMaxLength(120);
            e.Property(r => r.Description).HasColumnType("nvarchar(max)");
            e.Property(r => r.SourceUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<ServiceSchemaLookupSet>(e =>
        {
            e.HasIndex(s => s.Key).IsUnique();
            e.Property(s => s.Key).HasMaxLength(80);
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.Description).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ServiceSchemaLookupValue>(e =>
        {
            e.HasIndex(v => new { v.LookupSetId, v.Code }).IsUnique();
            e.Property(v => v.Code).HasMaxLength(80);
            e.Property(v => v.Label).HasMaxLength(200);
            e.Property(v => v.Description).HasColumnType("nvarchar(max)");
            e.HasOne(v => v.LookupSet)
                .WithMany(s => s.Values)
                .HasForeignKey(v => v.LookupSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CensusServiceLine>(e =>
        {
            e.HasIndex(s => s.Reference).IsUnique();
            e.Property(s => s.Reference).HasMaxLength(40);
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.Description).HasColumnType("nvarchar(max)");
            e.Property(s => s.BoundaryIn).HasColumnType("nvarchar(max)");
            e.Property(s => s.BoundaryOut).HasColumnType("nvarchar(max)");
            e.Property(s => s.StatusCode).HasMaxLength(40);
            e.Property(s => s.CreatedBy).HasMaxLength(255);
            e.Property(s => s.UpdatedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<CensusService>(e =>
        {
            e.HasIndex(s => s.Reference).IsUnique();
            e.Property(s => s.Reference).HasMaxLength(40);
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.PurposeNarrative).HasColumnType("nvarchar(max)");
            e.Property(s => s.Description).HasColumnType("nvarchar(max)");
            e.Property(s => s.BoundaryIn).HasColumnType("nvarchar(max)");
            e.Property(s => s.BoundaryOut).HasColumnType("nvarchar(max)");
            e.Property(s => s.ServiceTypeCode).HasMaxLength(80);
            e.Property(s => s.StatusCode).HasMaxLength(40);
            e.Property(s => s.CreatedBy).HasMaxLength(255);
            e.Property(s => s.UpdatedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<CensusServiceProduct>(e =>
        {
            e.Property(x => x.RelationshipCode).HasMaxLength(40);
            e.Property(x => x.Narrative).HasColumnType("nvarchar(max)");
            e.Property(x => x.CreatedBy).HasMaxLength(255);
            e.Property(x => x.RemovedBy).HasMaxLength(255);
            e.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ServiceId, x.ProductId, x.RelationshipCode });
        });

        modelBuilder.Entity<CensusServiceLineService>(e =>
        {
            e.Property(x => x.RelationshipCode).HasMaxLength(40);
            e.HasOne(x => x.ServiceLine)
                .WithMany()
                .HasForeignKey(x => x.ServiceLineId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CensusCatalogueItem>(e =>
        {
            e.HasIndex(x => new { x.Kind, x.Reference }).IsUnique();
            e.Property(x => x.Kind).HasMaxLength(40);
            e.Property(x => x.Reference).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.Statement).HasColumnType("nvarchar(max)");
            e.Property(x => x.LookupCode).HasMaxLength(80);
            e.Property(x => x.StatusCode).HasMaxLength(40);
            e.Property(x => x.CreatedBy).HasMaxLength(255);
            e.Property(x => x.UpdatedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<CensusEntry>(e =>
        {
            e.Property(x => x.DomainKey).HasMaxLength(40);
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.Narrative).HasColumnType("nvarchar(max)");
            e.Property(x => x.LookupCode).HasMaxLength(80);
            e.Property(x => x.SecondaryLookupCode).HasMaxLength(80);
            e.Property(x => x.PersonName).HasMaxLength(200);
            e.Property(x => x.PersonEmail).HasMaxLength(255);
            e.Property(x => x.ExternalUrl).HasMaxLength(500);
            e.Property(x => x.VerificationStatusCode).HasMaxLength(40);
            e.Property(x => x.CreatedBy).HasMaxLength(255);
            e.Property(x => x.UpdatedBy).HasMaxLength(255);
            e.Property(x => x.RemovedBy).HasMaxLength(255);
            e.Property(x => x.RemovalReason).HasMaxLength(500);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CatalogueItem)
                .WithMany()
                .HasForeignKey(x => x.CatalogueItemId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StaffRole)
                .WithMany()
                .HasForeignKey(x => x.StaffRoleId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ProductId, x.DomainKey });
        });

        modelBuilder.Entity<CensusSectionDeclaration>(e =>
        {
            e.Property(x => x.SectionKey).HasMaxLength(80);
            e.Property(x => x.StatusCode).HasMaxLength(40);
            e.Property(x => x.Explanation).HasColumnType("nvarchar(max)");
            e.Property(x => x.UpdatedBy).HasMaxLength(255);
            e.HasIndex(x => new { x.ProductId, x.SectionKey }).IsUnique();
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceSchemaAreaConfig>(e =>
        {
            e.HasIndex(a => a.Key).IsUnique();
            e.Property(a => a.Key).HasMaxLength(40);
            e.Property(a => a.Name).HasMaxLength(200);
            e.Property(a => a.Group).HasMaxLength(80);
            e.Property(a => a.Summary).HasMaxLength(4000);
            e.Property(a => a.HelpPanel);
            e.Property(a => a.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ServiceSchemaQuestion>(e =>
        {
            e.HasIndex(q => q.Key).IsUnique();
            e.HasIndex(q => q.AreaKey);
            e.Property(q => q.Key).HasMaxLength(40);
            e.Property(q => q.AreaKey).HasMaxLength(40);
            e.Property(q => q.Heading).HasMaxLength(200);
            e.Property(q => q.Help).HasMaxLength(4000);
            e.Property(q => q.HelpPanel);
            e.Property(q => q.TitleLabel).HasMaxLength(200);
            e.Property(q => q.NarrativeLabel).HasMaxLength(200);
            e.Property(q => q.ResponseMode).HasMaxLength(20);
            e.Property(q => q.LookupSource).HasMaxLength(40);
            e.Property(q => q.CatalogueKind).HasMaxLength(40);
            e.Property(q => q.ChoiceOptions).HasMaxLength(4000);
            e.Property(q => q.UpdatedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<ProjectContact>()
            .HasOne(pc => pc.StaffRole)
            .WithMany()
            .HasForeignKey(pc => pc.StaffRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
