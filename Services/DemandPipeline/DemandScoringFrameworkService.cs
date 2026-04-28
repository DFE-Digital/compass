using System.Text.Json;
using Compass.Data;
using Compass.Models.DemandPipeline;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.DemandPipeline;

public class DemandScoringFrameworkService : IDemandScoringFrameworkService
{
    private readonly CompassDbContext _db;

    public DemandScoringFrameworkService(CompassDbContext db)
    {
        _db = db;
    }

    public async Task EnsureDefaultFrameworkSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.DemandScoringBandDefinitions.AnyAsync(cancellationToken))
            return;

        DemandScoringFrameworkSeeder.Seed(_db);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DemandScoringFrameworkSnapshot> LoadActiveFrameworkAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultFrameworkSeededAsync(cancellationToken);

        var sections = await _db.DemandScoringFrameworkSections.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Include(s => s.Questions)
            .ThenInclude(q => q.Options)
            .ToListAsync(cancellationToken);

        foreach (var s in sections)
        {
            s.Questions = s.Questions.OrderBy(q => q.SortOrder).ToList();
            foreach (var q in s.Questions)
                q.Options = q.Options.OrderBy(o => o.SortOrder).ToList();
        }

        var bands = await _db.DemandScoringBandDefinitions.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(cancellationToken);

        return new DemandScoringFrameworkSnapshot { Sections = sections, Bands = bands };
    }

    public DemandScoringEvaluationResult EvaluateFromLegacyInts(
        DemandScoringFrameworkSnapshot framework,
        int? scoreStrategic,
        int? scoreUrgency,
        int? scoreFunding,
        int? scoreRice)
    {
        int MaxFor(string legacy) =>
            framework.Sections
                .Where(s => string.Equals(s.LegacyColumn, legacy, StringComparison.OrdinalIgnoreCase))
                .Sum(s => s.MaxPoints);

        var maxS = MaxFor("Strategic");
        var maxU = MaxFor("Urgency");
        var maxF = MaxFor("Funding");
        var maxR = MaxFor("Rice");

        var s = Math.Clamp(scoreStrategic ?? 0, 0, Math.Max(maxS, 0));
        var u = Math.Clamp(scoreUrgency ?? 0, 0, Math.Max(maxU, 0));
        var f = Math.Clamp(scoreFunding ?? 0, 0, Math.Max(maxF, 0));
        var r = Math.Clamp(scoreRice ?? 0, 0, Math.Max(maxR, 0));

        var rawTotal = s + u + f + r;
        var rawMax = framework.Sections.Sum(x => Math.Max(0, x.MaxPoints));
        if (rawMax == 0) rawMax = 1;

        var scaled = DemandScoringHelper.ScaleRawTo100(rawTotal, rawMax);
        var (bandCode, bandLabel) = ResolveBand(framework.Bands, scaled);

        return new DemandScoringEvaluationResult
        {
            RawTotal = rawTotal,
            RawMax = rawMax,
            Scaled100 = scaled,
            BandCode = bandCode,
            BandLabel = bandLabel,
            ScoreStrategic = s,
            ScoreUrgency = u,
            ScoreFunding = f,
            ScoreRice = r
        };
    }

    public DemandScoringEvaluationResult Evaluate(
        DemandScoringFrameworkSnapshot framework,
        IReadOnlyDictionary<string, string> answers)
    {
        var rawMax = framework.Sections.Sum(s => Math.Max(0, s.MaxPoints));
        if (rawMax == 0) rawMax = 1;

        var strategic = 0;
        var urgency = 0;
        var funding = 0;
        var rice = 0;

        var rawTotal = 0;

        foreach (var section in framework.Sections)
        {
            var sectionScore = 0;
            foreach (var q in section.Questions)
            {
                var t = (q.QuestionType ?? "Radio").Trim();
                answers.TryGetValue(q.Code, out var rawAnswer);
                var answer = rawAnswer?.Trim() ?? "";

                switch (t.ToLowerInvariant())
                {
                    case "context":
                        break;
                    case "text":
                        if (q.IsScored && int.TryParse(answer, out var tp))
                            sectionScore += tp;
                        break;
                    case "number":
                        if (!q.IsScored) break;
                        if (!int.TryParse(answer, out var n)) break;
                        var lo = q.NumberMin ?? 0;
                        var hi = q.NumberMax ?? int.MaxValue;
                        n = Math.Clamp(n, lo, hi);
                        sectionScore += n;
                        break;
                    case "radio":
                        if (!q.IsScored) break;
                        if (!int.TryParse(answer, out var optionId)) break;
                        var opt = q.Options.FirstOrDefault(o => o.Id == optionId);
                        if (opt != null) sectionScore += opt.Points;
                        break;
                    default:
                        break;
                }
            }

            sectionScore = Math.Clamp(sectionScore, 0, Math.Max(0, section.MaxPoints));
            rawTotal += sectionScore;

            AddLegacy(ref strategic, ref urgency, ref funding, ref rice, section.LegacyColumn, sectionScore);
        }

        var scaled = DemandScoringHelper.ScaleRawTo100(rawTotal, rawMax);
        var (bandCode, bandLabel) = ResolveBand(framework.Bands, scaled);

        return new DemandScoringEvaluationResult
        {
            RawTotal = rawTotal,
            RawMax = rawMax,
            Scaled100 = scaled,
            BandCode = bandCode,
            BandLabel = bandLabel,
            ScoreStrategic = strategic,
            ScoreUrgency = urgency,
            ScoreFunding = funding,
            ScoreRice = rice
        };
    }

    public string? ValidateScoringAnswersComplete(
        DemandScoringFrameworkSnapshot framework,
        IReadOnlyDictionary<string, string>? answers)
    {
        answers ??= new Dictionary<string, string>();
        foreach (var section in framework.Sections)
        {
            foreach (var q in section.Questions)
            {
                var t = (q.QuestionType ?? "Radio").Trim().ToLowerInvariant();
                if (t == "context") continue;
                if (!q.IsScored) continue;

                answers.TryGetValue(q.Code, out var rawAnswer);
                var answer = rawAnswer?.Trim() ?? "";

                switch (t)
                {
                    case "radio":
                        if (string.IsNullOrEmpty(answer) || !int.TryParse(answer, out var optionId)
                            || q.Options.All(o => o.Id != optionId))
                            return $"Answer every scored question before finalising. Missing: {q.Prompt}";
                        break;
                    case "number":
                        if (string.IsNullOrEmpty(answer) || !int.TryParse(answer, out var n))
                            return $"Answer every scored question before finalising. Missing: {q.Prompt}";
                        var lo = q.NumberMin ?? 0;
                        var hi = q.NumberMax ?? int.MaxValue;
                        if (n < lo || n > hi)
                            return $"Enter a valid number for: {q.Prompt} (between {lo} and {hi}).";
                        break;
                    case "text":
                        if (string.IsNullOrWhiteSpace(answer))
                            return $"Answer every scored question before finalising. Missing: {q.Prompt}";
                        break;
                }
            }
        }

        return null;
    }

    private static void AddLegacy(ref int s, ref int u, ref int f, ref int r, string? legacy, int points)
    {
        if (string.IsNullOrWhiteSpace(legacy)) return;
        switch (legacy.Trim().ToLowerInvariant())
        {
            case "strategic": s += points; break;
            case "urgency": u += points; break;
            case "funding": f += points; break;
            case "rice": r += points; break;
        }
    }

    private static (string? Code, string? Label) ResolveBand(IReadOnlyList<DemandScoringBandDefinition> bands, int scaled100)
    {
        if (bands.Count == 0)
            return (DemandScoringHelper.BandFromScaled100(scaled100), DemandScoringHelper.BandLabel(DemandScoringHelper.BandFromScaled100(scaled100)));

        foreach (var b in bands.OrderBy(x => x.SortOrder))
        {
            if (scaled100 >= b.MinScaledInclusive && scaled100 <= b.MaxScaledInclusive)
                return (b.Code, b.Label);
        }

        var fallback = bands.OrderByDescending(x => x.MaxScaledInclusive).First();
        return (fallback.Code, fallback.Label);
    }

    public static Dictionary<string, string>? ParseAnswersJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch
        {
            return null;
        }
    }
}
