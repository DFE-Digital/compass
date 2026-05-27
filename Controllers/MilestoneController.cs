using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers
{
    [Authorize]
    public class MilestoneController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<MilestoneController> _logger;

        public MilestoneController(CompassDbContext context, ILogger<MilestoneController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Milestone/MilestoneDetails/5
        public async Task<IActionResult> MilestoneDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var milestone = await _context.Milestones
                .Include(m => m.Project)
                .Include(m => m.Objective)
                .Include(m => m.MilestoneActions)
                    .ThenInclude(ma => ma.Action)
                .Include(m => m.MilestoneRisks)
                    .ThenInclude(mr => mr.Risk)
                .Include(m => m.MilestoneIssues)
                    .ThenInclude(mi => mi.Issue)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (milestone == null)
            {
                return NotFound();
            }

            return View(milestone);
        }

        // POST: Milestone/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string? description, DateTime dueDate, string status, DateTime? actualDate, int? progressPercent, string? notes)
        {
            try
            {
                var milestone = await _context.Milestones
                    .Include(m => m.Project)
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

                if (milestone == null)
                {
                    return NotFound();
                }

                milestone.Name = name;
                milestone.Description = description;
                milestone.DueDate = dueDate;
                milestone.Status = status;
                milestone.ActualDate = actualDate;
                milestone.ProgressPercent = progressPercent;
                milestone.Notes = notes;
                milestone.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Milestone updated successfully.";
                if (milestone.ProjectId.HasValue)
                    return RedirectToAction("Detail", "ModernWork", new { id = milestone.ProjectId.Value, tab = "milestones" });
                return RedirectToAction("AllWork", "ModernWork");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating milestone {MilestoneId}", id);
                TempData["ErrorMessage"] = "Error updating milestone. Please try again.";
                var failedMilestone = await _context.Milestones.AsNoTracking()
                    .Where(m => m.Id == id)
                    .Select(m => m.ProjectId)
                    .FirstOrDefaultAsync();
                if (failedMilestone.HasValue)
                    return RedirectToAction("Detail", "ModernWork", new { id = failedMilestone.Value, tab = "milestones" });
                return RedirectToAction("AllWork", "ModernWork");
            }
        }

        // POST: Milestone/UpdateField
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateField(int id, string field, string? name, string? description, DateTime? dueDate, string? status, DateTime? actualDate, int? progressPercent, string? notes, string? businessArea)
        {
            try
            {
                var milestone = await _context.Milestones
                    .Include(m => m.Project)
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

                if (milestone == null)
                {
                    return NotFound();
                }

                // Get current user info
                var userEmail = User.Identity?.Name ?? "system@example.com";
                var userName = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                // Track changes for milestone update
                var previousStatus = milestone.Status;
                var previousProgress = milestone.ProgressPercent;
                string updateDetails = "";

                // Update only the specified field
                switch (field)
                {
                    case "name":
                        updateDetails = $"Name changed from '{milestone.Name}' to '{name}'";
                        milestone.Name = name ?? milestone.Name;
                        break;
                    case "status":
                        updateDetails = $"Status changed from '{previousStatus.Replace("_", " ")}' to '{status?.Replace("_", " ")}'";
                        milestone.Status = status ?? milestone.Status;
                        break;
                    case "description":
                        updateDetails = "Description updated";
                        milestone.Description = description;
                        break;
                    case "dueDate":
                        updateDetails = $"Due date changed from {milestone.DueDate:dd MMM yyyy} to {dueDate:dd MMM yyyy}";
                        if (dueDate.HasValue)
                            milestone.DueDate = dueDate.Value;
                        break;
                    case "actualDate":
                        updateDetails = actualDate.HasValue 
                            ? $"Actual date set to {actualDate:dd MMM yyyy}" 
                            : "Actual date cleared";
                        milestone.ActualDate = actualDate;
                        break;
                    case "progress":
                        updateDetails = $"Progress changed from {previousProgress ?? 0}% to {progressPercent ?? 0}%";
                        milestone.ProgressPercent = progressPercent;
                        break;
                    case "notes":
                        updateDetails = "Notes updated";
                        milestone.Notes = notes;
                        break;
                    case "businessArea":
                        updateDetails = $"Business area changed to '{businessArea}'";
                        milestone.BusinessArea = businessArea;
                        break;
                }

                // Create milestone update record
                var milestoneUpdate = new MilestoneUpdate
                {
                    MilestoneId = id,
                    UpdateDetails = updateDetails,
                    PreviousStatus = field == "status" ? previousStatus : null,
                    NewStatus = field == "status" ? status : null,
                    PreviousProgress = field == "progress" ? previousProgress : null,
                    NewProgress = field == "progress" ? progressPercent : null,
                    UpdatedByEmail = userEmail,
                    UpdatedByName = userName,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MilestoneUpdates.Add(milestoneUpdate);
                milestone.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{field} updated successfully.";
                if (milestone.ProjectId.HasValue)
                    return RedirectToAction("Detail", "ModernWork", new { id = milestone.ProjectId.Value, tab = "milestones" });
                return RedirectToAction("AllWork", "ModernWork");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating milestone field {Field} for milestone {MilestoneId}", field, id);
                TempData["ErrorMessage"] = "Error updating milestone. Please try again.";
                var failedMilestone = await _context.Milestones.AsNoTracking()
                    .Where(m => m.Id == id)
                    .Select(m => m.ProjectId)
                    .FirstOrDefaultAsync();
                if (failedMilestone.HasValue)
                    return RedirectToAction("Detail", "ModernWork", new { id = failedMilestone.Value, tab = "milestones" });
                return RedirectToAction("AllWork", "ModernWork");
            }
        }

        // POST: Milestone/AddUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUpdate(int milestoneId, string updateDetails, string? newStatus, int? newProgress)
        {
            try
            {
                var milestone = await _context.Milestones
                    .FirstOrDefaultAsync(m => m.Id == milestoneId && !m.IsDeleted);

                if (milestone == null)
                {
                    return NotFound();
                }

                var projectId = milestone.ProjectId;
                var userEmail = User.Identity?.Name ?? "system@example.com";
                var userName = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                // Create milestone update record
                var milestoneUpdate = new MilestoneUpdate
                {
                    MilestoneId = milestoneId,
                    UpdateDetails = updateDetails,
                    PreviousStatus = milestone.Status,
                    NewStatus = !string.IsNullOrEmpty(newStatus) ? newStatus : null,
                    PreviousProgress = milestone.ProgressPercent,
                    NewProgress = newProgress,
                    UpdatedByEmail = userEmail,
                    UpdatedByName = userName,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MilestoneUpdates.Add(milestoneUpdate);

                // Update milestone if status or progress changed
                if (!string.IsNullOrEmpty(newStatus))
                {
                    milestone.Status = newStatus;
                }

                if (newProgress.HasValue)
                {
                    milestone.ProgressPercent = newProgress;
                }

                milestone.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Milestone update added successfully.";
                if (projectId.HasValue)
                    return RedirectToAction("Detail", "ModernWork", new { id = projectId.Value, tab = "milestones" });
                return RedirectToAction("AllWork", "ModernWork");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding milestone update for milestone {MilestoneId}", milestoneId);
                TempData["ErrorMessage"] = "Error adding milestone update. Please try again.";
                var failedProjectId = await _context.Milestones.AsNoTracking()
                    .Where(m => m.Id == milestoneId)
                    .Select(m => m.ProjectId)
                    .FirstOrDefaultAsync();
                if (failedProjectId.HasValue)
                    return RedirectToAction("Detail", "ModernWork", new { id = failedProjectId.Value, tab = "milestones" });
                return RedirectToAction("AllWork", "ModernWork");
            }
        }

        // POST: Milestone/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var milestone = await _context.Milestones
                    .Include(m => m.Project)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (milestone == null)
                {
                    return NotFound();
                }

                var projectId = milestone.ProjectId;
                
                milestone.IsDeleted = true;
                milestone.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Milestone deleted successfully.";
                
                if (projectId.HasValue)
                {
                    return RedirectToAction("Detail", "ModernWork", new { id = projectId.Value, tab = "milestones" });
                }
                
                return RedirectToAction("AllWork", "ModernWork");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting milestone {MilestoneId}", id);
                TempData["ErrorMessage"] = "Error deleting milestone. Please try again.";
                return RedirectToAction(nameof(MilestoneDetails), new { id = id });
            }
        }
    }
}
