using FipsReporting.Data;
using Microsoft.EntityFrameworkCore;

namespace FipsReporting.Services
{
    public interface IUserService
    {
        Task<ReportingUser?> GetUserByEmailAsync(string email);
        Task<ReportingUser> CreateUserAsync(ReportingUser user, string createdBy);
        Task<ReportingUser> UpdateUserAsync(ReportingUser user, string updatedBy);
        Task<List<ReportingUser>> GetUsersByRoleAsync(string role);
        Task<bool> IsUserAdminAsync(string email);
        Task<bool> IsUserReportingUserAsync(string email);
    }

    public class UserService : IUserService
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ReportingDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReportingUser?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.ReportingUsers
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                throw;
            }
        }

        public async Task<ReportingUser> CreateUserAsync(ReportingUser user, string createdBy)
        {
            try
            {
                user.CreatedBy = createdBy;
                user.CreatedAt = DateTime.UtcNow;

                _context.ReportingUsers.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserEmail} created by {CreatedBy}", user.Email, createdBy);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {UserEmail}", user.Email);
                throw;
            }
        }

        public async Task<ReportingUser> UpdateUserAsync(ReportingUser user, string updatedBy)
        {
            try
            {
                _context.ReportingUsers.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserEmail} updated by {UpdatedBy}", user.Email, updatedBy);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserEmail}", user.Email);
                throw;
            }
        }

        public async Task<List<ReportingUser>> GetUsersByRoleAsync(string role)
        {
            try
            {
                return await _context.ReportingUsers
                    .Where(u => u.Role == role && u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role {Role}", role);
                throw;
            }
        }

        public async Task<bool> IsUserAdminAsync(string email)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                return user?.Role == "admin" || user?.Role == "central_operations";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {Email} is admin", email);
                throw;
            }
        }

        public async Task<bool> IsUserReportingUserAsync(string email)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                return user?.Role == "reporting_user" || user?.Role == "admin" || user?.Role == "central_operations";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {Email} is reporting user", email);
                throw;
            }
        }
    }
}
