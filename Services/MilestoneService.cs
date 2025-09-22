using Microsoft.EntityFrameworkCore;
using FipsReporting.Data;
using FipsReporting.Models;

namespace FipsReporting.Services
{
    public interface IMilestoneService
    {
        Task<List<Milestone>> GetMilestonesByFipsIdAsync(string fipsId);
        Task<List<Milestone>> GetMilestonesForProductAsync(string fipsId); // Alias for GetMilestonesByFipsIdAsync
        Task<Milestone?> GetMilestoneByIdAsync(int id);
        Task<Milestone> CreateMilestoneAsync(Milestone milestone);
        Task<Milestone> CreateMilestoneAsync(Milestone milestone, string userEmail); // Overload with user email
        Task<Milestone> UpdateMilestoneAsync(Milestone milestone);
        Task<Milestone> UpdateMilestoneAsync(Milestone milestone, string userEmail); // Overload with user email
        Task DeleteMilestoneAsync(int id);
        Task<List<MilestoneUpdate>> GetMilestoneUpdatesAsync(int milestoneId);
        Task<MilestoneUpdate> AddMilestoneUpdateAsync(MilestoneUpdate update);
        Task<MilestoneUpdate> AddMilestoneUpdateAsync(int milestoneId, string updateText, string userEmail); // Overload for controllers
        Task<string> CalculateRagStatusAsync(Milestone milestone);
        Task<List<Milestone>> GetOverdueMilestonesAsync();
        Task<List<Milestone>> GetMilestonesByStatusAsync(string status);
    }

    public class MilestoneService : IMilestoneService
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<MilestoneService> _logger;

        public MilestoneService(ReportingDbContext context, ILogger<MilestoneService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Milestone>> GetMilestonesByFipsIdAsync(string fipsId)
        {
            var milestones = await _context.Milestones
                .Where(m => m.FipsId == fipsId)
                .Include(m => m.Objective)
                .Include(m => m.Updates)
                .OrderBy(m => m.TargetDate)
                .ToListAsync();
            
            // Sync alias properties
            foreach (var milestone in milestones)
            {
                SyncMilestoneAliases(milestone);
            }
            
            return milestones;
        }

        public async Task<List<Milestone>> GetMilestonesForProductAsync(string fipsId)
        {
            return await GetMilestonesByFipsIdAsync(fipsId);
        }

        public async Task<Milestone?> GetMilestoneByIdAsync(int id)
        {
            var milestone = await _context.Milestones
                .Include(m => m.Objective)
                .Include(m => m.Updates.OrderByDescending(u => u.UpdateDate))
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (milestone != null)
            {
                SyncMilestoneAliases(milestone);
            }
            
            return milestone;
        }

        public async Task<Milestone> CreateMilestoneAsync(Milestone milestone)
        {
            milestone.CreatedDate = DateTime.UtcNow;
            milestone.CreatedAt = DateTime.UtcNow;
            _context.Milestones.Add(milestone);
            await _context.SaveChangesAsync();
            return milestone;
        }

        public async Task<Milestone> CreateMilestoneAsync(Milestone milestone, string userEmail)
        {
            milestone.CreatedBy = userEmail;
            return await CreateMilestoneAsync(milestone);
        }

        public async Task<Milestone> UpdateMilestoneAsync(Milestone milestone)
        {
            milestone.LastUpdatedDate = DateTime.UtcNow;
            milestone.UpdatedAt = DateTime.UtcNow;
            _context.Milestones.Update(milestone);
            await _context.SaveChangesAsync();
            return milestone;
        }

        public async Task<Milestone> UpdateMilestoneAsync(Milestone milestone, string userEmail)
        {
            milestone.LastUpdatedBy = userEmail;
            return await UpdateMilestoneAsync(milestone);
        }

        public async Task DeleteMilestoneAsync(int id)
        {
            var milestone = await _context.Milestones.FindAsync(id);
            if (milestone != null)
            {
                _context.Milestones.Remove(milestone);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<MilestoneUpdate>> GetMilestoneUpdatesAsync(int milestoneId)
        {
            return await _context.MilestoneUpdates
                .Where(u => u.MilestoneId == milestoneId)
                .OrderByDescending(u => u.UpdateDate)
                .ToListAsync();
        }

        public async Task<MilestoneUpdate> AddMilestoneUpdateAsync(MilestoneUpdate update)
        {
            update.UpdateDate = DateTime.UtcNow;
            update.UpdatedAt = DateTime.UtcNow;
            _context.MilestoneUpdates.Add(update);
            await _context.SaveChangesAsync();
            return update;
        }

        public async Task<MilestoneUpdate> AddMilestoneUpdateAsync(int milestoneId, string updateText, string userEmail)
        {
            var update = new MilestoneUpdate
            {
                MilestoneId = milestoneId,
                UpdateText = updateText,
                UpdatedBy = userEmail
            };
            return await AddMilestoneUpdateAsync(update);
        }

        public async Task<string> CalculateRagStatusAsync(Milestone milestone)
        {
            // Calculate RAG status based on milestone status and target date
            if (milestone.Status == "Completed")
                return "Green";
            
            if (milestone.Status == "Cancelled")
                return "Grey";
            
            if (milestone.TargetDate.HasValue)
            {
                var daysUntilTarget = (milestone.TargetDate.Value - DateTime.Now).Days;
                
                if (daysUntilTarget < 0) // Overdue
                    return "Red";
                else if (daysUntilTarget <= 7) // Due soon
                    return "Amber";
                else
                    return "Green";
            }
            
            return "Amber"; // Default for milestones without target dates
        }

        public async Task<List<Milestone>> GetOverdueMilestonesAsync()
        {
            var today = DateTime.Now.Date;
            var milestones = await _context.Milestones
                .Where(m => m.TargetDate.HasValue && 
                           m.TargetDate.Value.Date < today && 
                           m.Status != "Completed" && 
                           m.Status != "Cancelled")
                .Include(m => m.Objective)
                .OrderBy(m => m.TargetDate)
                .ToListAsync();
            
            // Sync alias properties
            foreach (var milestone in milestones)
            {
                SyncMilestoneAliases(milestone);
            }
            
            return milestones;
        }

        public async Task<List<Milestone>> GetMilestonesByStatusAsync(string status)
        {
            var milestones = await _context.Milestones
                .Where(m => m.Status == status)
                .Include(m => m.Objective)
                .OrderBy(m => m.TargetDate)
                .ToListAsync();
            
            // Sync alias properties
            foreach (var milestone in milestones)
            {
                SyncMilestoneAliases(milestone);
            }
            
            return milestones;
        }

        private void SyncMilestoneAliases(Milestone milestone)
        {
            // Sync alias properties to ensure compatibility with existing code
            milestone.ProductId = milestone.FipsId;
            milestone.DueDate = milestone.TargetDate;
            milestone.CreatedAt = milestone.CreatedDate;
            milestone.UpdatedAt = milestone.LastUpdatedDate;
            
            // Calculate RAG status
            milestone.RagStatus = CalculateRagStatus(milestone);
            
            // Sync MilestoneUpdate aliases
            foreach (var update in milestone.Updates)
            {
                update.UpdatedAt = update.UpdateDate;
            }
        }

        private string CalculateRagStatus(Milestone milestone)
        {
            // Calculate RAG status based on milestone status and target date
            if (milestone.Status == "Completed")
                return "Green";
            
            if (milestone.Status == "Cancelled")
                return "Grey";
            
            if (milestone.TargetDate.HasValue)
            {
                var daysUntilTarget = (milestone.TargetDate.Value - DateTime.Now).Days;
                
                if (daysUntilTarget < 0) // Overdue
                    return "Red";
                else if (daysUntilTarget <= 7) // Due soon
                    return "Amber";
                else
                    return "Green";
            }
            
            return "Amber"; // Default for milestones without target dates
        }
    }
}