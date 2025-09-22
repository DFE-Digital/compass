using Microsoft.EntityFrameworkCore;
using FipsReporting.Data;

namespace FipsReporting.Services
{
    public interface IObjectiveService
    {
        Task<List<Objective>> GetAllObjectivesAsync();
        Task<Objective?> GetObjectiveByIdAsync(int id);
        Task<Objective?> GetObjectiveByReferenceAsync(string reference);
        Task<Objective> CreateObjectiveAsync(Objective objective);
        Task<Objective> UpdateObjectiveAsync(Objective objective);
        Task DeleteObjectiveAsync(int id);
        Task<List<Objective>> GetObjectivesByTypeAsync(string type);
        Task<List<Objective>> GetObjectivesByStatusAsync(string status);
    }

    public class ObjectiveService : IObjectiveService
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<ObjectiveService> _logger;

        public ObjectiveService(ReportingDbContext context, ILogger<ObjectiveService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Objective>> GetAllObjectivesAsync()
        {
            return await _context.Objectives
                .Include(o => o.Milestones)
                .OrderBy(o => o.Reference)
                .ToListAsync();
        }

        public async Task<Objective?> GetObjectiveByIdAsync(int id)
        {
            return await _context.Objectives
                .Include(o => o.Milestones)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Objective?> GetObjectiveByReferenceAsync(string reference)
        {
            return await _context.Objectives
                .Include(o => o.Milestones)
                .FirstOrDefaultAsync(o => o.Reference == reference);
        }

        public async Task<Objective> CreateObjectiveAsync(Objective objective)
        {
            objective.CreatedAt = DateTime.UtcNow;
            objective.UpdatedAt = DateTime.UtcNow;
            _context.Objectives.Add(objective);
            await _context.SaveChangesAsync();
            return objective;
        }

        public async Task<Objective> UpdateObjectiveAsync(Objective objective)
        {
            objective.UpdatedAt = DateTime.UtcNow;
            _context.Objectives.Update(objective);
            await _context.SaveChangesAsync();
            return objective;
        }

        public async Task DeleteObjectiveAsync(int id)
        {
            var objective = await _context.Objectives.FindAsync(id);
            if (objective != null)
            {
                _context.Objectives.Remove(objective);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Objective>> GetObjectivesByTypeAsync(string type)
        {
            return await _context.Objectives
                .Where(o => o.Type == type)
                .Include(o => o.Milestones)
                .OrderBy(o => o.Reference)
                .ToListAsync();
        }

        public async Task<List<Objective>> GetObjectivesByStatusAsync(string status)
        {
            return await _context.Objectives
                .Where(o => o.Status == status)
                .Include(o => o.Milestones)
                .OrderBy(o => o.Reference)
                .ToListAsync();
        }
    }
}