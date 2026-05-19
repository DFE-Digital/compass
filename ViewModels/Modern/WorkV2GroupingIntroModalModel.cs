namespace Compass.ViewModels.Modern;

/// <summary>First-visit “how to use” intro for Work V2 directorates / business areas registers.</summary>
public enum WorkV2GroupingIntroKind
{
    Directorates,
    BusinessAreas,
    ByTheme,
}

public sealed record WorkV2GroupingIntroModalModel(WorkV2GroupingIntroKind Kind);
