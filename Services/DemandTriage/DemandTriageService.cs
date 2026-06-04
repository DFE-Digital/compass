using Compass.Data;
using Compass.Models;
using Compass.Models.DemandTriage;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Compass.Services.DemandTriage;

public class DemandTriageService : IDemandTriageService
{
    private readonly CompassDbContext _db;
    private readonly ILogger<DemandTriageService> _logger;
    private readonly IWorkItemNotificationService _workItemNotifications;

    public DemandTriageService(
        CompassDbContext db,
        ILogger<DemandTriageService> logger,
        IWorkItemNotificationService workItemNotifications)
    {
        _db = db;
        _logger = logger;
        _workItemNotifications = workItemNotifications;
    }

    // ── Reference generation ─────────────────────────────────────────────────

    public async Task<string> GenerateNextReferenceAsync()
    {
        var max = await _db.DemandTriageRequests
            .AsNoTracking()
            .OrderByDescending(r => r.Id)
            .Select(r => r.RequestReference)
            .FirstOrDefaultAsync();

        int next = 1;
        if (max != null && max.StartsWith("DR-", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(max[3..], out var parsed))
                next = parsed + 1;
        }

        return $"DR-{next:D6}";
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<DemandTriageRequest> CreateDraftAsync(string ownerEmail, string ownerName)
    {
        var reference = await GenerateNextReferenceAsync();

        var request = new DemandTriageRequest
        {
            RequestReference = reference,
            Status = DemandTriageStatus.Draft,
            OwnerUserEmail = ownerEmail,
            OwnerUserName = ownerName,
            CreatedBy = ownerEmail,
            UpdatedBy = ownerEmail,
        };

        _db.DemandTriageRequests.Add(request);
        await _db.SaveChangesAsync();

        await WriteAuditAsync(request.Id, DemandAuditActions.Created, null, DemandTriageStatus.Draft,
            ownerEmail, ownerName, null);

        return request;
    }

    public async Task<DemandTriageRequest?> GetByIdAsync(int id, bool includeAll = true)
    {
        var query = _db.DemandTriageRequests.AsQueryable();

        if (includeAll)
        {
            query = query
                .Include(r => r.ExploratoryReview)
                .Include(r => r.Scorecard)
                    .ThenInclude(s => s!.Answers)
                .Include(r => r.TriageOutcome)
                .Include(r => r.AuditEvents.OrderByDescending(e => e.OccurredAt))
                .Include(r => r.BusinessCase)
                .Include(r => r.ConvertedProject);
        }

        return await query.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);
    }

    public async Task<List<DemandTriageRequest>> GetAllAsync(string? statusFilter = null, string? ownerEmail = null)
    {
        var query = _db.DemandTriageRequests
            .AsNoTracking()
            .Include(r => r.Scorecard)
            .Include(r => r.TriageOutcome)
            .Where(r => r.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(r => r.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(ownerEmail))
            query = query.Where(r => r.OwnerUserEmail == ownerEmail);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task UpdateCaptureFormAsync(int id, DemandTriageRequest updates, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests.FindAsync(id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertEditableStatus(request);

        var before = Snapshot(request);

        request.RequestName = updates.RequestName;
        request.RequesterFullName = updates.RequesterFullName;
        request.DepartmentGroup = updates.DepartmentGroup;
        request.DdtPortfolioSupport = updates.DdtPortfolioSupport;
        request.PointsOfContact = updates.PointsOfContact;
        request.SroName = updates.SroName;
        request.ProposedRequestTitle = updates.ProposedRequestTitle;
        request.RequestOverview = updates.RequestOverview;
        request.PreviousResearch = updates.PreviousResearch;
        request.ManifestoOrStatute = updates.ManifestoOrStatute;
        request.SosOpportunityMissionPillars = updates.SosOpportunityMissionPillars;
        request.DdtStrategicTheme = updates.DdtStrategicTheme;
        request.ExpectedBenefits = updates.ExpectedBenefits;
        request.RiskConsequence = updates.RiskConsequence;
        request.FundingProvided = updates.FundingProvided;
        request.HeadcountProvided = updates.HeadcountProvided;
        request.HeadcountDetails = updates.HeadcountDetails;
        request.TargetDeliveryDate = updates.TargetDeliveryDate;
        request.NewOrChangedDigitalService = updates.NewOrChangedDigitalService;
        request.NewOrChangedServiceDetails = updates.NewOrChangedServiceDetails;
        request.PublicFacingDigitalService = updates.PublicFacingDigitalService;
        request.BusinessCaseId = updates.BusinessCaseId;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.Updated, request.Status, request.Status,
            actorEmail, actorName, before);
    }

    // ── Workflow transitions ─────────────────────────────────────────────────

    public async Task SubmitAsync(int id, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests.FindAsync(id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertTransition(request, DemandTriageStatus.Submitted,
            DemandTriageStatus.Draft, DemandTriageStatus.ReturnedForClarification);

        var errors = await ValidateForSubmissionAsync(id);
        if (errors.Any())
            throw new ValidationException(errors);

        var before = Snapshot(request);
        var fromStatus = request.Status;

        request.Status = DemandTriageStatus.Submitted;
        request.SubmittedAt = DateTime.UtcNow;
        request.SubmittedBy = actorEmail;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.Submitted, fromStatus, DemandTriageStatus.Submitted,
            actorEmail, actorName, before);
    }

    public async Task StartExploratoryReviewAsync(int id, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests
            .Include(r => r.ExploratoryReview)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertTransition(request, DemandTriageStatus.ExploratoryReviewInProgress,
            DemandTriageStatus.Submitted);

        var before = Snapshot(request);
        var fromStatus = request.Status;

        request.Status = DemandTriageStatus.ExploratoryReviewInProgress;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        if (request.ExploratoryReview == null)
        {
            request.ExploratoryReview = new DemandExploratoryReview
            {
                DemandTriageRequestId = id,
                CreatedBy = actorEmail,
                UpdatedBy = actorEmail
            };
        }

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.ExploratoryStarted, fromStatus,
            DemandTriageStatus.ExploratoryReviewInProgress, actorEmail, actorName, before);
    }

    public async Task SaveExploratoryReviewAsync(int id, DemandExploratoryReview data, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests
            .Include(r => r.ExploratoryReview)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        if (request.Status != DemandTriageStatus.ExploratoryReviewInProgress)
            throw new InvalidOperationException("Exploratory review can only be saved when in progress.");

        var review = request.ExploratoryReview ?? new DemandExploratoryReview
        {
            DemandTriageRequestId = id,
            CreatedBy = actorEmail
        };

        review.SummaryFindings = data.SummaryFindings;
        review.KeyRisks = data.KeyRisks;
        review.Dependencies = data.Dependencies;
        review.RecommendationToProceed = data.RecommendationToProceed;
        review.ReasonNotProceeding = data.ReasonNotProceeding;
        review.UpdatedBy = actorEmail;
        review.UpdatedAt = DateTime.UtcNow;

        if (request.ExploratoryReview == null)
            _db.DemandExploratoryReviews.Add(review);

        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
    }

    public async Task CompleteExploratoryReviewAsync(int id, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests
            .Include(r => r.ExploratoryReview)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertTransition(request, DemandTriageStatus.ExploratoryReviewComplete,
            DemandTriageStatus.ExploratoryReviewInProgress);

        var review = request.ExploratoryReview
            ?? throw new InvalidOperationException("No exploratory review found.");

        if (string.IsNullOrWhiteSpace(review.SummaryFindings))
            throw new ValidationException(new[] { "Summary findings are required." });
        if (string.IsNullOrWhiteSpace(review.KeyRisks))
            throw new ValidationException(new[] { "Key risks are required." });
        if (review.RecommendationToProceed == null)
            throw new ValidationException(new[] { "Recommendation to proceed is required." });

        var before = Snapshot(request);
        var fromStatus = request.Status;

        review.CompletedAt = DateTime.UtcNow;
        review.CompletedBy = actorEmail;
        review.UpdatedAt = DateTime.UtcNow;
        review.UpdatedBy = actorEmail;

        request.Status = DemandTriageStatus.ExploratoryReviewComplete;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.ExploratoryCompleted, fromStatus,
            DemandTriageStatus.ExploratoryReviewComplete, actorEmail, actorName, before);
    }

    public async Task ReturnForClarificationAsync(int id, string reason, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests.FindAsync(id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        if (request.Status != DemandTriageStatus.ExploratoryReviewInProgress &&
            request.Status != DemandTriageStatus.ScoringInProgress)
            throw new InvalidOperationException("Can only return for clarification from exploratory review or scoring in progress.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException(new[] { "A reason for returning is required." });

        var before = Snapshot(request);
        var fromStatus = request.Status;

        request.Status = DemandTriageStatus.ReturnedForClarification;
        request.ReturnReason = reason;
        request.ReturnedBy = actorEmail;
        request.ReturnedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.Returned, fromStatus,
            DemandTriageStatus.ReturnedForClarification, actorEmail, actorName, before, reason);
    }

    public async Task StartScoringAsync(int id, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests
            .Include(r => r.Scorecard)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertTransition(request, DemandTriageStatus.ScoringInProgress,
            DemandTriageStatus.ExploratoryReviewComplete);

        var before = Snapshot(request);
        var fromStatus = request.Status;

        request.Status = DemandTriageStatus.ScoringInProgress;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        if (request.Scorecard == null)
        {
            request.Scorecard = new DemandScorecard
            {
                DemandTriageRequestId = id,
                ScorecardStatus = ScorecardStatusValues.InProgress,
                CreatedBy = actorEmail,
                UpdatedBy = actorEmail
            };
        }

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.ScoringStarted, fromStatus,
            DemandTriageStatus.ScoringInProgress, actorEmail, actorName, before);
    }

    public async Task SaveAnswersAsync(int id, List<AnswerInput> answers, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests
            .Include(r => r.Scorecard)
                .ThenInclude(s => s!.Answers)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        if (request.Status != DemandTriageStatus.ScoringInProgress)
            throw new InvalidOperationException("Answers can only be saved when scoring is in progress.");

        var scorecard = request.Scorecard
            ?? throw new InvalidOperationException("No scorecard found. Start scoring first.");

        if (scorecard.IsLocked)
            throw new InvalidOperationException("Scorecard is locked. Unlock it before making changes.");

        // Remove existing answers for the question codes being updated
        var codesBeingUpdated = answers.Select(a => a.QuestionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var toRemove = scorecard.Answers
            .Where(a => codesBeingUpdated.Contains(a.QuestionCode, StringComparer.OrdinalIgnoreCase))
            .ToList();
        _db.DemandAnswers.RemoveRange(toRemove);

        // Add new answers
        foreach (var input in answers)
        {
            var score = DemandScoringEngine.ScoreAnswer(input.QuestionCode, input.AnswerValue ?? string.Empty);
            scorecard.Answers.Add(new DemandAnswer
            {
                DemandScorecardId = scorecard.Id,
                QuestionCode = input.QuestionCode,
                AnswerValue = input.AnswerValue,
                AnswerScore = score,
                FreeText = input.FreeText,
                CreatedBy = actorEmail,
                UpdatedBy = actorEmail
            });
        }

        // Recalculate scores
        var allAnswers = scorecard.Answers
            .Where(a => !toRemove.Contains(a))
            .ToList();

        var result = DemandScoringEngine.Calculate(allAnswers);
        ApplyCalculationResult(scorecard, result);

        scorecard.ScorecardStatus = ScorecardStatusValues.InProgress;
        scorecard.UpdatedAt = DateTime.UtcNow;
        scorecard.UpdatedBy = actorEmail;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
    }

    public async Task<ScorecardCalculationResult> FinaliseScorecard(int id, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests
            .Include(r => r.ExploratoryReview)
            .Include(r => r.Scorecard)
                .ThenInclude(s => s!.Answers)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertTransition(request, DemandTriageStatus.ScoredFinalised,
            DemandTriageStatus.ScoringInProgress);

        var errors = await ValidateForScoringFinalisationAsync(id);
        if (errors.Any())
            throw new ValidationException(errors);

        var scorecard = request.Scorecard!;
        var before = Snapshot(request);
        var fromStatus = request.Status;

        var result = DemandScoringEngine.Calculate(scorecard.Answers);
        ApplyCalculationResult(scorecard, result);

        scorecard.IsLocked = true;
        scorecard.ScorecardStatus = ScorecardStatusValues.Finalised;
        scorecard.FinalisedAt = DateTime.UtcNow;
        scorecard.FinalisedBy = actorEmail;
        scorecard.UpdatedAt = DateTime.UtcNow;
        scorecard.UpdatedBy = actorEmail;

        request.Status = DemandTriageStatus.ScoredFinalised;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.ScoringFinalised, fromStatus,
            DemandTriageStatus.ScoredFinalised, actorEmail, actorName, before);

        return result;
    }

    public async Task SendToTriageAsync(int id, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests.FindAsync(id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertTransition(request, DemandTriageStatus.TriagePending,
            DemandTriageStatus.ScoredFinalised);

        var before = Snapshot(request);
        var fromStatus = request.Status;

        request.Status = DemandTriageStatus.TriagePending;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.TriageSent, fromStatus,
            DemandTriageStatus.TriagePending, actorEmail, actorName, before);
    }

    public async Task RecordTriageOutcomeAsync(int id, DemandTriageOutcome outcome, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests
            .Include(r => r.Scorecard)
            .Include(r => r.TriageOutcome)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertTransition(request, DemandTriageStatus.Triaged,
            DemandTriageStatus.TriagePending);

        if (string.IsNullOrWhiteSpace(outcome.OutcomeSelection))
            throw new ValidationException(new[] { "Outcome selection is required." });
        if (string.IsNullOrWhiteSpace(outcome.OutcomeSummary))
            throw new ValidationException(new[] { "Outcome summary is required." });
        if (string.IsNullOrWhiteSpace(outcome.RoutedToArea))
            throw new ValidationException(new[] { "Routed to area is required." });

        // Check if override is needed
        var suggestion = request.Scorecard?.SuggestionBand;
        bool overrideNeeded = IsOverrideNeeded(outcome.OutcomeSelection, suggestion);
        if (overrideNeeded && string.IsNullOrWhiteSpace(outcome.OverrideReason))
            throw new ValidationException(new[] { "An override reason is required because the outcome conflicts with the suggestion band." });

        var before = Snapshot(request);
        var fromStatus = request.Status;

        var existing = request.TriageOutcome;
        if (existing == null)
        {
            existing = new DemandTriageOutcome
            {
                DemandTriageRequestId = id,
                CreatedBy = actorEmail
            };
            _db.DemandTriageOutcomes.Add(existing);
        }

        existing.OutcomeSelection = outcome.OutcomeSelection;
        existing.OutcomeSummary = outcome.OutcomeSummary;
        existing.RoutedToArea = outcome.RoutedToArea;
        existing.OverrodeRecommendation = overrideNeeded;
        existing.OverrideReason = overrideNeeded ? outcome.OverrideReason : null;
        existing.DecidedAt = DateTime.UtcNow;
        existing.DecidedBy = actorEmail;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = actorEmail;

        request.Status = DemandTriageStatus.Triaged;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();

        var auditAction = overrideNeeded ? DemandAuditActions.TriageOverrideUsed : DemandAuditActions.TriageRecorded;
        await WriteAuditAsync(id, auditAction, fromStatus, DemandTriageStatus.Triaged,
            actorEmail, actorName, before,
            overrideNeeded ? $"Override: {outcome.OverrideReason}" : null);
    }

    public async Task CloseAsync(int id, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests.FindAsync(id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        AssertTransition(request, DemandTriageStatus.Closed, DemandTriageStatus.Triaged);

        var before = Snapshot(request);
        var fromStatus = request.Status;

        request.Status = DemandTriageStatus.Closed;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.Closed, fromStatus,
            DemandTriageStatus.Closed, actorEmail, actorName, before);
    }

    public async Task AdminOverrideStatusAsync(int id, string newStatus, string reason, string actorEmail, string actorName)
    {
        if (!DemandTriageStatus.All.Contains(newStatus))
            throw new ArgumentException($"Invalid status: {newStatus}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException(new[] { "A reason is required for admin status override." });

        var request = await _db.DemandTriageRequests.FindAsync(id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        var before = Snapshot(request);
        var fromStatus = request.Status;

        request.Status = newStatus;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.AdminOverride, fromStatus,
            newStatus, actorEmail, actorName, before, reason);
    }

    public async Task UnlockScorecardAsync(int id, string actorEmail, string actorName)
    {
        var request = await _db.DemandTriageRequests
            .Include(r => r.Scorecard)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        var scorecard = request.Scorecard
            ?? throw new InvalidOperationException("No scorecard found.");

        scorecard.IsLocked = false;
        scorecard.ScorecardStatus = ScorecardStatusValues.InProgress;
        scorecard.UpdatedAt = DateTime.UtcNow;
        scorecard.UpdatedBy = actorEmail;

        request.Status = DemandTriageStatus.ScoringInProgress;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;

        await _db.SaveChangesAsync();
        await WriteAuditAsync(id, DemandAuditActions.ScorecardUnlocked, DemandTriageStatus.ScoredFinalised,
            DemandTriageStatus.ScoringInProgress, actorEmail, actorName, null);
    }

    // ── Project creation ─────────────────────────────────────────────────────

    public async Task<int> CreateProjectFromDemandAsync(int id, string actorEmail, string actorName)
    {
        var request = await GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Demand request {id} not found.");

        if (request.ConvertedProjectId.HasValue)
            throw new InvalidOperationException("A project has already been created from this demand.");

        var project = new Project
        {
            Title = request.ProposedRequestTitle ?? request.RequestName ?? request.RequestReference,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        request.ConvertedProjectId = project.Id;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = actorEmail;
        await _db.SaveChangesAsync();

        await WriteAuditAsync(id, DemandAuditActions.ProjectCreated, request.Status, request.Status,
            actorEmail, actorName, null, $"Project ID: {project.Id}");

        try
        {
            await _workItemNotifications.TrySendWorkItemCreatedAsync(
                project.Id,
                actorEmail,
                actorName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send work item created notification for project {ProjectId}", project.Id);
        }

        return project.Id;
    }

    // ── Validation ───────────────────────────────────────────────────────────

    public async Task<List<string>> ValidateForSubmissionAsync(int id)
    {
        var request = await _db.DemandTriageRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return new List<string> { "Request not found." };

        var errors = new List<string>();

        void Require(string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                errors.Add($"{label} is required.");
        }

        Require(request.RequestName, "Request name");
        Require(request.RequesterFullName, "Full name");
        Require(request.DepartmentGroup, "Department group");
        Require(request.PointsOfContact, "Points of contact");
        Require(request.SroName, "Senior responsible officer");
        Require(request.ProposedRequestTitle, "Proposed request title");
        Require(request.RequestOverview, "Request overview");
        Require(request.ExpectedBenefits, "Expected measurable benefits");
        Require(request.RiskConsequence, "Risk / consequence");

        if (request.TargetDeliveryDate == null)
            errors.Add("Target delivery date is required.");

        if (request.NewOrChangedDigitalService == true && string.IsNullOrWhiteSpace(request.NewOrChangedServiceDetails))
            errors.Add("Details of the new or changed digital service are required.");

        return errors;
    }

    public async Task<List<string>> ValidateForScoringFinalisationAsync(int id)
    {
        var request = await _db.DemandTriageRequests
            .AsNoTracking()
            .Include(r => r.ExploratoryReview)
            .Include(r => r.Scorecard)
                .ThenInclude(s => s!.Answers)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return new List<string> { "Request not found." };

        var errors = new List<string>();

        if (request.Status != DemandTriageStatus.ScoringInProgress)
            errors.Add("Status must be 'scoring in progress'.");

        if (request.ExploratoryReview?.CompletedAt == null)
            errors.Add("Exploratory review must be completed before finalising the scorecard.");

        var scorecard = request.Scorecard;
        if (scorecard == null)
        {
            errors.Add("No scorecard found.");
            return errors;
        }

        var answers = scorecard.Answers.ToList();

        string? GetAnswer(string code) =>
            answers.FirstOrDefault(a => string.Equals(a.QuestionCode, code, StringComparison.OrdinalIgnoreCase))?.AnswerValue;

        bool HasAnswer(string code) => answers.Any(a =>
            string.Equals(a.QuestionCode, code, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(a.AnswerValue));

        bool HasFreeText(string code) => answers.Any(a =>
            string.Equals(a.QuestionCode, code, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(a.FreeText));

        // Required scored questions
        foreach (var code in new[] { "1.2", "1.3", "1.4", "2.1", "2.3", "3.2", "4.4", "4.6", "4.7", "4.8", "4.9" })
        {
            if (!HasAnswer(code))
                errors.Add($"Question {code} must be answered.");
        }

        // Conditional: 2.1 = Yes → 2.2 free text required
        if (string.Equals(GetAnswer("2.1"), "Yes", StringComparison.OrdinalIgnoreCase) && !HasFreeText("2.2"))
            errors.Add("Question 2.2: compelling reason detail is required when 2.1 = Yes.");

        // Conditional: 2.3 != "No critical delivery date" → 2.4 date required
        var q23 = GetAnswer("2.3");
        if (!string.IsNullOrWhiteSpace(q23) && !string.Equals(q23, "No critical delivery date", StringComparison.OrdinalIgnoreCase))
        {
            if (!HasFreeText("2.4"))
                errors.Add("Question 2.4: critical delivery date is required.");
        }
        else if (string.Equals(q23, "No critical delivery date", StringComparison.OrdinalIgnoreCase))
        {
            if (!HasFreeText("2.5"))
                errors.Add("Question 2.5: target delivery date or more info is required when no critical date.");
        }

        // Conditional: 3.2 = Yes → 3.3, 3.4, 3.5, 3.6 required
        if (string.Equals(GetAnswer("3.2"), "Yes", StringComparison.OrdinalIgnoreCase))
        {
            if (!HasAnswer("3.3")) errors.Add("Question 3.3 is required when funding is confirmed.");
            if (!HasFreeText("3.4")) errors.Add("Question 3.4: funding area is required when funding is confirmed.");
            if (!HasAnswer("3.5")) errors.Add("Question 3.5 is required when funding is confirmed.");
            if (!HasAnswer("3.6")) errors.Add("Question 3.6: funding type is required when funding is confirmed.");
        }

        // Conditional: 4.1 drives 4.2/4.3 requirements
        var q41 = GetAnswer("4.1");
        if (!string.IsNullOrWhiteSpace(q41))
        {
            bool needExternal = string.Equals(q41, "External users", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(q41, "Both", StringComparison.OrdinalIgnoreCase);
            bool needInternal = string.Equals(q41, "Internal users", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(q41, "Both", StringComparison.OrdinalIgnoreCase);

            if (needExternal && !HasAnswer("4.2"))
                errors.Add("Question 4.2: external user groups are required.");
            if (needInternal && !HasAnswer("4.3"))
                errors.Add("Question 4.3: internal user groups are required.");
        }

        // Conditional: 4.9 = Yes → 4.10 free text required
        if (string.Equals(GetAnswer("4.9"), "Yes", StringComparison.OrdinalIgnoreCase) && !HasFreeText("4.10"))
            errors.Add("Question 4.10: headcount details are required when headcount is provided.");

        return errors;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void AssertEditableStatus(DemandTriageRequest request)
    {
        if (request.Status != DemandTriageStatus.Draft &&
            request.Status != DemandTriageStatus.ReturnedForClarification)
            throw new InvalidOperationException(
                $"Cannot edit a request in status '{request.Status}'. Only draft or returned requests can be edited.");
    }

    private static void AssertTransition(DemandTriageRequest request, string toStatus, params string[] allowedFromStatuses)
    {
        if (!allowedFromStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Cannot transition from '{request.Status}' to '{toStatus}'. " +
                $"Allowed from: {string.Join(", ", allowedFromStatuses)}.");
    }

    private static void ApplyCalculationResult(DemandScorecard scorecard, ScorecardCalculationResult result)
    {
        scorecard.StrategicAlignmentScore = result.StrategicAlignmentScore;
        scorecard.UrgencyScore = result.UrgencyScore;
        scorecard.FundingScore = result.FundingScore;
        scorecard.RiceScore = result.RiceScore;
        scorecard.TotalScore = result.TotalScore;
        scorecard.StrategicAlignmentBand = result.StrategicAlignmentBand;
        scorecard.UrgencyBand = result.UrgencyBand;
        scorecard.FundingBand = result.FundingBand;
        scorecard.RiceBand = result.RiceBand;
        scorecard.SuggestionBand = result.SuggestionBand;
    }

    private static bool IsOverrideNeeded(string outcomeSelection, string? suggestionBand)
    {
        if (string.IsNullOrWhiteSpace(suggestionBand)) return false;

        // "Must do" → only "Progress to next stage" is aligned
        if (suggestionBand == "Must do" && outcomeSelection != TriageOutcomeValues.ProgressToNextStage)
            return true;

        // "Do not do" → only "Reject" or "Pause" is aligned
        if (suggestionBand == "Do not do" &&
            outcomeSelection != TriageOutcomeValues.Reject &&
            outcomeSelection != TriageOutcomeValues.Pause)
            return true;

        return false;
    }

    private static string? Snapshot(object? obj)
    {
        if (obj == null) return null;
        try
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            });
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteAuditAsync(int requestId, string action, string? fromStatus, string? toStatus,
        string actorEmail, string actorName, string? beforeJson, string? notes = null)
    {
        try
        {
            _db.DemandTriageAuditEvents.Add(new DemandTriageAuditEvent
            {
                DemandTriageRequestId = requestId,
                Action = action,
                ActorEmail = actorEmail,
                ActorDisplayName = actorName,
                OccurredAt = DateTime.UtcNow,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                BeforeJson = beforeJson,
                Notes = notes
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit event {Action} for demand {Id}", action, requestId);
        }
    }
}

/// <summary>Thrown when business validation fails.</summary>
public class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors) : base(string.Join("; ", errors))
    {
        Errors = errors.ToList();
    }
}
