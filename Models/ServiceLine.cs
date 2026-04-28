using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Compass.Models.Fips;

namespace Compass.Models;

/// <summary>Groups directorates, business areas, and linked service register / work for portfolio reporting.</summary>
public class ServiceLine
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ServiceLineDivision> ServiceLineDivisions { get; set; } = new List<ServiceLineDivision>();
    public ICollection<ServiceLineBusinessArea> ServiceLineBusinessAreas { get; set; } = new List<ServiceLineBusinessArea>();
    public ICollection<ServiceLineProduct> ServiceLineProducts { get; set; } = new List<ServiceLineProduct>();
    public ICollection<ServiceLineProject> ServiceLineProjects { get; set; } = new List<ServiceLineProject>();
}

[Table("ServiceLineDivisions")]
public class ServiceLineDivision
{
    [Required] public Guid ServiceLineId { get; set; }
    public ServiceLine ServiceLine { get; set; } = null!;

    [Required] public int DivisionId { get; set; }
    public Division Division { get; set; } = null!;
}

[Table("ServiceLineBusinessAreas")]
public class ServiceLineBusinessArea
{
    [Required] public Guid ServiceLineId { get; set; }
    public ServiceLine ServiceLine { get; set; } = null!;

    [Required] public int BusinessAreaLookupId { get; set; }
    public BusinessAreaLookup BusinessAreaLookup { get; set; } = null!;
}

[Table("ServiceLineProducts")]
public class ServiceLineProduct
{
    [Required] public Guid ServiceLineId { get; set; }
    public ServiceLine ServiceLine { get; set; } = null!;

    [Required] public Guid CMDBProductId { get; set; }
    public CMDBProduct CMDBProduct { get; set; } = null!;
}

[Table("ServiceLineProjects")]
public class ServiceLineProject
{
    [Required] public Guid ServiceLineId { get; set; }
    public ServiceLine ServiceLine { get; set; } = null!;

    [Required] public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}
