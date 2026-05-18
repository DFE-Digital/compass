using Compass.Data;
using Compass.Models.DemandPipeline;

namespace Compass.Services.DemandPipeline;

/// <summary>Inserts the default demand prioritisation scorecard (legacy 15+10+22+42, bands 57/21/0).</summary>
public static class DemandScoringFrameworkSeeder
{
    public static void Seed(CompassDbContext db)
    {
        db.DemandScoringBandDefinitions.AddRange(
            new DemandScoringBandDefinition { Code = "DoNotDo", Label = "Do not do", MinScaledInclusive = 0, MaxScaledInclusive = 20, SortOrder = 0, IsActive = true },
            new DemandScoringBandDefinition { Code = "CouldDo", Label = "Could do", MinScaledInclusive = 21, MaxScaledInclusive = 56, SortOrder = 1, IsActive = true },
            new DemandScoringBandDefinition { Code = "MustDo", Label = "Must do", MinScaledInclusive = 57, MaxScaledInclusive = 100, SortOrder = 2, IsActive = true });

        var strategic = new DemandScoringFrameworkSection
        {
            Key = "strategic",
            Title = "Section 1 — DDT strategic alignment",
            Description = "Align the demand to strategic outcomes, mission pillars, and portfolio roadmaps.",
            MaxPoints = 15,
            SortOrder = 0,
            IsActive = true,
            LegacyColumn = "Strategic"
        };

        strategic.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S1_CTX_PROBLEM",
            Prompt = "Q1.1 What problem is this request solving?",
            Hint = "Context only — not scored.",
            QuestionType = "Context",
            ContextKey = "Description",
            IsScored = false,
            SortOrder = 0
        });
        strategic.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S1_CTX_EXPLORE",
            Prompt = "From Explore — aim clarification",
            Hint = null,
            QuestionType = "Context",
            ContextKey = "ExploreAimClarification",
            IsScored = false,
            SortOrder = 1
        });
        strategic.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S1Q2",
            Prompt = "Q1.2 Which DDT priority strategic outcome does this best support?",
            Hint = "Submission shows the selected priority outcome where relevant.",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 2,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Delivering digitally — joined-up services (+5)", Points = 5, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "Powered by data — data-driven school system (+5)", Points = 5, SortOrder = 1 },
                new DemandScoringFrameworkOption { Label = "The right technology — unified platforms (+5)", Points = 5, SortOrder = 2 },
                new DemandScoringFrameworkOption { Label = "None of the above (+0)", Points = 0, SortOrder = 3 }
            }
        });
        strategic.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S1Q3",
            Prompt = "Q1.3 Which SoS opportunity mission pillar does this best support?",
            Hint = "Submission shows the selected mission pillar where relevant.",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 3,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Best start in life (+5)", Points = 5, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "Every child achieving and thriving (+5)", Points = 5, SortOrder = 1 },
                new DemandScoringFrameworkOption { Label = "Skills for opportunity and growth (+5)", Points = 5, SortOrder = 2 },
                new DemandScoringFrameworkOption { Label = "Cross-cutting — family security (+5)", Points = 5, SortOrder = 3 },
                new DemandScoringFrameworkOption { Label = "Does not support a mission pillar (+0)", Points = 0, SortOrder = 4 }
            }
        });
        strategic.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S1Q4",
            Prompt = "Q1.4 Is this request on a DDT portfolio roadmap?",
            Hint = "Portfolio is shown from the submission.",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 4,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Yes — named on a roadmap (+5)", Points = 5, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "Aligned but not explicitly listed (+2)", Points = 2, SortOrder = 1 },
                new DemandScoringFrameworkOption { Label = "Not on any roadmap (+0)", Points = 0, SortOrder = 2 }
            }
        });
        strategic.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S1Q4A_TEXT",
            Prompt = "Q1.4a If yes, which roadmap and how does this align?",
            Hint = "Context only — not scored.",
            QuestionType = "Text",
            IsScored = false,
            SortOrder = 5
        });

        var urgency = new DemandScoringFrameworkSection
        {
            Key = "urgency",
            Title = "Section 2 — Urgency",
            Description = "Assess urgency and critical delivery dates.",
            MaxPoints = 10,
            SortOrder = 1,
            IsActive = true,
            LegacyColumn = "Urgency"
        };
        urgency.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S2Q1",
            Prompt = "Q2.1 Is there a compelling reason for DDT to act now?",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 0,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Yes — clear urgent driver (+5)", Points = 5, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "Unclear — some signals (+2)", Points = 2, SortOrder = 1 },
                new DemandScoringFrameworkOption { Label = "No compelling reason (+0)", Points = 0, SortOrder = 2 }
            }
        });
        urgency.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S2Q2_TEXT",
            Prompt = "Q2.2 Describe the compelling reason (if applicable)",
            QuestionType = "Text",
            IsScored = false,
            SortOrder = 1
        });
        urgency.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S2Q3",
            Prompt = "Q2.3 Is there a critical delivery date?",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 2,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Contract / procurement deadline (+5)", Points = 5, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "Legislative or statutory deadline (+5)", Points = 5, SortOrder = 1 },
                new DemandScoringFrameworkOption { Label = "Ministerial commitment (+5)", Points = 5, SortOrder = 2 },
                new DemandScoringFrameworkOption { Label = "Programme or portfolio milestone (+5)", Points = 5, SortOrder = 3 },
                new DemandScoringFrameworkOption { Label = "No critical delivery date (+0)", Points = 0, SortOrder = 4 }
            }
        });
        urgency.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S2Q4_TEXT",
            Prompt = "Q2.4 What is the critical delivery date?",
            QuestionType = "Text",
            IsScored = false,
            SortOrder = 3
        });

        var funding = new DemandScoringFrameworkSection
        {
            Key = "funding",
            Title = "Section 3 — Funding and resource",
            Description = "Funding, budget, and MSP resource confirmation.",
            MaxPoints = 22,
            SortOrder = 2,
            IsActive = true,
            LegacyColumn = "Funding"
        };
        funding.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S3_CTX_TYPE",
            Prompt = "Q3.1 What type of request is this? (context — not scored)",
            QuestionType = "Context",
            ContextKey = "DeliveryTypeSummary",
            IsScored = false,
            SortOrder = 0
        });
        funding.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S3Q2",
            Prompt = "Q3.2 Does the request have confirmed funding?",
            Hint = "Submission shows funding status.",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 1,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Yes — funding confirmed (+10)", Points = 10, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "Partially confirmed (+5)", Points = 5, SortOrder = 1 },
                new DemandScoringFrameworkOption { Label = "Not yet confirmed (+0)", Points = 0, SortOrder = 2 }
            }
        });
        funding.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S3Q3",
            Prompt = "Q3.3 Budget approved for programme or single phase?",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 2,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Entire programme (+5)", Points = 5, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "Single phase only (+2)", Points = 2, SortOrder = 1 },
                new DemandScoringFrameworkOption { Label = "Not yet defined (+0)", Points = 0, SortOrder = 2 }
            }
        });
        funding.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S3Q5",
            Prompt = "Q3.5 Cost centre / budget code identified?",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 3,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Yes (+2)", Points = 2, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "No (+0)", Points = 0, SortOrder = 1 }
            }
        });
        funding.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S3Q5A_TEXT",
            Prompt = "Q3.5a If yes, enter cost centre",
            QuestionType = "Text",
            IsScored = false,
            SortOrder = 4
        });
        funding.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "S3Q6",
            Prompt = "Q3.6 Is MSP resource confirmed?",
            Hint = "Submission shows headcount status.",
            QuestionType = "Radio",
            IsScored = true,
            SortOrder = 5,
            Options =
            {
                new DemandScoringFrameworkOption { Label = "Yes — fully confirmed (+5)", Points = 5, SortOrder = 0 },
                new DemandScoringFrameworkOption { Label = "Partial (+2)", Points = 2, SortOrder = 1 },
                new DemandScoringFrameworkOption { Label = "No (+0)", Points = 0, SortOrder = 2 }
            }
        });

        var rice = new DemandScoringFrameworkSection
        {
            Key = "rice",
            Title = "Section 4 — RICE",
            Description = "Reach (0–16), impact (0–10), confidence and effort (0–16). Total must not exceed 42.",
            MaxPoints = 42,
            SortOrder = 3,
            IsActive = true,
            LegacyColumn = "Rice"
        };
        rice.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "RICE_R",
            Prompt = "Reach — who and how many are affected?",
            QuestionType = "Number",
            IsScored = true,
            NumberMin = 0,
            NumberMax = 16,
            SortOrder = 0
        });
        rice.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "RICE_I",
            Prompt = "Impact — scale of benefit",
            QuestionType = "Number",
            IsScored = true,
            NumberMin = 0,
            NumberMax = 10,
            SortOrder = 1
        });
        rice.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "RICE_E",
            Prompt = "Confidence and effort — deliverability",
            QuestionType = "Number",
            IsScored = true,
            NumberMin = 0,
            NumberMax = 16,
            SortOrder = 2
        });
        rice.Questions.Add(new DemandScoringFrameworkQuestion
        {
            Code = "RICE_NOTES_TEXT",
            Prompt = "Notes on delivery complexity or dependencies",
            QuestionType = "Text",
            IsScored = false,
            SortOrder = 3
        });

        db.DemandScoringFrameworkSections.AddRange(strategic, urgency, funding, rice);
    }
}
