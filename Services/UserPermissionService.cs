using FipsReporting.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FipsReporting.Services
{
    public interface IUserPermissionService
    {
        Task<UserPermission?> GetUserPermissionAsync(string email);
        Task<List<UserPermission>> GetAllUserPermissionsAsync();
        Task<UserPermission> CreateUserPermissionAsync(UserPermission userPermission, string createdBy);
        Task UpdateUserPermissionAsync(UserPermission userPermission, string updatedBy);
        Task DeleteUserPermissionAsync(int id);
        Task<bool> HasPermissionAsync(string email, string permission);
        Task<List<string>> GetUserPermissionsAsync(string email);
        Task SeedSuperAdminAsync();
    }

    public class UserPermissionService : IUserPermissionService
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<UserPermissionService> _logger;

        public UserPermissionService(ReportingDbContext context, ILogger<UserPermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserPermission?> GetUserPermissionAsync(string email)
        {
            try
            {
                return await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.Email == email && up.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permission for {Email}", email);
                throw;
            }
        }

        public async Task<List<UserPermission>> GetAllUserPermissionsAsync()
        {
            try
            {
                return await _context.UserPermissions
                    .Where(up => up.IsActive)
                    .OrderBy(up => up.Email)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user permissions");
                throw;
            }
        }

        public async Task<UserPermission> CreateUserPermissionAsync(UserPermission userPermission, string createdBy)
        {
            try
            {
                userPermission.CreatedBy = createdBy;
                userPermission.CreatedAt = DateTime.UtcNow;
                userPermission.UpdatedAt = DateTime.UtcNow;

                _context.UserPermissions.Add(userPermission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created user permission for {Email} by {CreatedBy}", 
                    userPermission.Email, createdBy);

                return userPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user permission for {Email}", userPermission.Email);
                throw;
            }
        }

        public async Task UpdateUserPermissionAsync(UserPermission userPermission, string updatedBy)
        {
            try
            {
                userPermission.UpdatedBy = updatedBy;
                userPermission.UpdatedAt = DateTime.UtcNow;

                _context.UserPermissions.Update(userPermission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user permission for {Email} by {UpdatedBy}", 
                    userPermission.Email, updatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user permission for {Email}", userPermission.Email);
                throw;
            }
        }

        public async Task DeleteUserPermissionAsync(int id)
        {
            try
            {
                var userPermission = await _context.UserPermissions.FindAsync(id);
                if (userPermission != null)
                {
                    userPermission.IsActive = false;
                    userPermission.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deactivated user permission for {Email}", userPermission.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user permission with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> HasPermissionAsync(string email, string permission)
        {
            try
            {
                var userPermission = await GetUserPermissionAsync(email);
                if (userPermission == null)
                    return false;

                return permission switch
                {
                    "AddProduct" => userPermission.CanAddProduct,
                    "EditProduct" => userPermission.CanEditProduct,
                    "DeleteProduct" => userPermission.CanDeleteProduct,
                    "AddMetric" => userPermission.CanAddMetric,
                    "EditMetric" => userPermission.CanEditMetric,
                    "DeleteMetric" => userPermission.CanDeleteMetric,
                    "AddMilestone" => userPermission.CanAddMilestone,
                    "EditMilestone" => userPermission.CanEditMilestone,
                    "DeleteMilestone" => userPermission.CanDeleteMilestone,
                    "AddUser" => userPermission.CanAddUser,
                    "EditUser" => userPermission.CanEditUser,
                    "ViewReports" => userPermission.CanViewReports,
                    "SubmitReports" => userPermission.CanSubmitReports,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for {Email}", permission, email);
                return false;
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(string email)
        {
            try
            {
                var userPermission = await GetUserPermissionAsync(email);
                if (userPermission == null)
                    return new List<string>();

                var permissions = new List<string>();
                
                if (userPermission.CanAddProduct) permissions.Add("AddProduct");
                if (userPermission.CanEditProduct) permissions.Add("EditProduct");
                if (userPermission.CanDeleteProduct) permissions.Add("DeleteProduct");
                if (userPermission.CanAddMetric) permissions.Add("AddMetric");
                if (userPermission.CanEditMetric) permissions.Add("EditMetric");
                if (userPermission.CanDeleteMetric) permissions.Add("DeleteMetric");
                if (userPermission.CanAddMilestone) permissions.Add("AddMilestone");
                if (userPermission.CanEditMilestone) permissions.Add("EditMilestone");
                if (userPermission.CanDeleteMilestone) permissions.Add("DeleteMilestone");
                if (userPermission.CanAddUser) permissions.Add("AddUser");
                if (userPermission.CanEditUser) permissions.Add("EditUser");
                if (userPermission.CanViewReports) permissions.Add("ViewReports");
                if (userPermission.CanSubmitReports) permissions.Add("SubmitReports");

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for {Email}", email);
                return new List<string>();
            }
        }

        public async Task SeedSuperAdminAsync()
        {
            try
            {
                var superAdminEmail = "andy.jones@education.gov.uk";
                var existingSuperAdmin = await GetUserPermissionAsync(superAdminEmail);
                
                if (existingSuperAdmin == null)
                {
                    var superAdmin = new UserPermission
                    {
                        Email = superAdminEmail,
                        Name = "Andy Jones",
                        IsActive = true,
                        CanAddProduct = true,
                        CanEditProduct = true,
                        CanDeleteProduct = true,
                        CanAddMetric = true,
                        CanEditMetric = true,
                        CanDeleteMetric = true,
                        CanAddMilestone = true,
                        CanEditMilestone = true,
                        CanDeleteMilestone = true,
                        CanAddUser = true,
                        CanEditUser = true,
                        CanViewReports = true,
                        CanSubmitReports = true,
                        CreatedBy = "System",
                        UpdatedBy = "System"
                    };

                    await CreateUserPermissionAsync(superAdmin, "System");
                    _logger.LogInformation("Seeded super admin user: {Email}", superAdminEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding super admin user");
                throw;
            }
        }
    }
}
