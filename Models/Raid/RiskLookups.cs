namespace Compass.Models;

public class RiskStatus : RaidLookupBase { }

public class RiskPriority : RaidLookupBase { }

/// <summary>Likelihood band for RAID risks; <see cref="MatrixScore"/> feeds inherent score (likelihood × impact).</summary>
public class RiskLikelihood : RaidLookupBase
{
    /// <summary>Numeric weight for scoring (typically 1–5). Configurable in Admin → RAID → Risk likelihoods.</summary>
    public int MatrixScore { get; set; } = 3;
}

/// <summary>Impact band for RAID risks; <see cref="MatrixScore"/> feeds inherent score (likelihood × impact).</summary>
public class RiskImpactLevel : RaidLookupBase
{
    /// <summary>Numeric weight for scoring (typically 1–5). Configurable in Admin → RAID → Risk impact levels.</summary>
    public int MatrixScore { get; set; } = 3;
}

public class RiskProximity : RaidLookupBase { }

public class RiskCategory : RaidLookupBase { }

public class RiskTreatment : RaidLookupBase { }
