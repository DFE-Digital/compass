namespace Compass.ViewModels.Modern;

public class AdminFipsUserGroupEditViewModel
{
    public bool IsCreate { get; set; }
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentId { get; set; }
    public string? ParentPath { get; set; }
    public int ChildCount { get; set; }
    public List<AdminFipsUserGroupParentOption> ParentOptions { get; set; } = new();
    public List<AdminFipsUserGroupSynonymRow> Synonyms { get; set; } = new();

    public string PageHeading => IsCreate
        ? (ParentId.HasValue ? "Add child user group" : "Add user group")
        : $"Edit user group";
}

public class AdminFipsUserGroupSynonymRow
{
    public int Id { get; set; }
    public string Synonym { get; set; } = string.Empty;
}
