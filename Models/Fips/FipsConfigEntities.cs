using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.Fips;

public class FipsBusinessArea
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>When set, this FIPS row mirrors <see cref="Compass.Models.BusinessAreaLookup"/> (single source of truth for admin).</summary>
    public int? BusinessAreaLookupId { get; set; }

    public Compass.Models.BusinessAreaLookup? BusinessAreaLookup { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
}

public class FipsDirectorate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>When set, this FIPS row mirrors <see cref="Compass.Models.DirectorateLookup"/>.</summary>
    public int? DirectorateLookupId { get; set; }

    public Compass.Models.DirectorateLookup? DirectorateLookup { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
}

public class FipsChannel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
}

public class FipsType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
}

public class FipsUserGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;

    public int? ParentId { get; set; }
    public FipsUserGroup? Parent { get; set; }
    public ICollection<FipsUserGroup> Children { get; set; } = new List<FipsUserGroup>();
    public ICollection<FipsUserGroupSynonym> Synonyms { get; set; } = new List<FipsUserGroupSynonym>();
}

public class FipsUserGroupSynonym
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FipsUserGroupId { get; set; }
    public FipsUserGroup FipsUserGroup { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Synonym { get; set; } = string.Empty;
}

public class FipsContactRole
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool AllowMultiple { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>Admin-defined categorisation dimension for FIPS products (e.g. portfolio strand).</summary>
public class FipsCategorisationGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<FipsCategorisationItem> Items { get; set; } = new List<FipsCategorisationItem>();
}

/// <summary>Value within a <see cref="FipsCategorisationGroup"/>; assignable on CMDB products.</summary>
public class FipsCategorisationItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FipsCategorisationGroupId { get; set; }

    [ForeignKey(nameof(FipsCategorisationGroupId))]
    public FipsCategorisationGroup Group { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
}
