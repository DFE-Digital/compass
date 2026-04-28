using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.Fips;

public enum CMDBProductStatus
{
    New = 0,
    Active = 1,
    Inactive = 2,
    /// <summary>Excluded by sync rules (e.g. title pattern); still updated from CMDB unless manually handled.</summary>
    Rejected = 3
}

public class CMDBProduct
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UniqueID { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public string? CMDBDescription { get; set; }
    public string? UserDescription { get; set; }

    [MaxLength(100)]
    public string? CMDBID { get; set; }

    [MaxLength(2000)]
    public string? ProductURL { get; set; }

    public CMDBProductStatus Status { get; set; } = CMDBProductStatus.New;

    /// <summary>Last JSON snapshot of the CMDB service-offering row (used for rules and future reporting).</summary>
    public string? LastCmdbSnapshotJson { get; set; }

    public int? PhaseId { get; set; }
    public PhaseLookup? Phase { get; set; }

    public ICollection<CMDBProductBusinessArea> BusinessAreas { get; set; } = new List<CMDBProductBusinessArea>();
    public ICollection<CMDBProductChannel> Channels { get; set; } = new List<CMDBProductChannel>();
    public ICollection<CMDBProductUserGroup> UserGroups { get; set; } = new List<CMDBProductUserGroup>();
    public ICollection<CMDBProductType> Types { get; set; } = new List<CMDBProductType>();
    public ICollection<CMDBProductFipsCategorisationItem> CategorisationItems { get; set; } =
        new List<CMDBProductFipsCategorisationItem>();
    public ICollection<CMDBProductContact> Contacts { get; set; } = new List<CMDBProductContact>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}

public class CMDBProductBusinessArea
{
    [Key]
    public int Id { get; set; }
    public Guid CMDBProductId { get; set; }
    public CMDBProduct CMDBProduct { get; set; } = null!;
    public int FipsBusinessAreaId { get; set; }
    public FipsBusinessArea FipsBusinessArea { get; set; } = null!;
}

public class CMDBProductChannel
{
    [Key]
    public int Id { get; set; }
    public Guid CMDBProductId { get; set; }
    public CMDBProduct CMDBProduct { get; set; } = null!;
    public int FipsChannelId { get; set; }
    public FipsChannel FipsChannel { get; set; } = null!;
}

public class CMDBProductUserGroup
{
    [Key]
    public int Id { get; set; }
    public Guid CMDBProductId { get; set; }
    public CMDBProduct CMDBProduct { get; set; } = null!;
    public int FipsUserGroupId { get; set; }
    public FipsUserGroup FipsUserGroup { get; set; } = null!;
}

public class CMDBProductType
{
    [Key]
    public int Id { get; set; }
    public Guid CMDBProductId { get; set; }
    public CMDBProduct CMDBProduct { get; set; } = null!;
    public int FipsTypeId { get; set; }
    public FipsType FipsType { get; set; } = null!;
}

public class CMDBProductFipsCategorisationItem
{
    [Key]
    public int Id { get; set; }
    public Guid CMDBProductId { get; set; }
    public CMDBProduct CMDBProduct { get; set; } = null!;
    public int FipsCategorisationItemId { get; set; }
    public FipsCategorisationItem FipsCategorisationItem { get; set; } = null!;
}

public class CMDBProductContact
{
    [Key]
    public int Id { get; set; }
    public Guid CMDBProductId { get; set; }
    public CMDBProduct CMDBProduct { get; set; } = null!;
    public int FipsContactRoleId { get; set; }
    public FipsContactRole FipsContactRole { get; set; } = null!;

    [MaxLength(320)]
    public string? UserEmail { get; set; }

    [MaxLength(200)]
    public string? UserName { get; set; }

    public bool CanManage { get; set; }
}
