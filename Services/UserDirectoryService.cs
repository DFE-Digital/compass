using Compass.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using GraphUser = Microsoft.Graph.Models.User;
using UserEntity = Compass.Models.User;

namespace Compass.Services;

public class UserDirectoryService : IUserDirectoryService
{
    private static readonly string[] UserSelectFields = new[]
    {
        "id",
        "displayName",
        "givenName",
        "surname",
        "mail",
        "userPrincipalName",
        "jobTitle"
    };

    private readonly CompassDbContext _context;
    private readonly GraphServiceClient _graph;
    private readonly ILogger<UserDirectoryService> _logger;

    public UserDirectoryService(
        CompassDbContext context,
        GraphServiceClient graph,
        ILogger<UserDirectoryService> logger)
    {
        _context = context;
        _graph = graph;
        _logger = logger;
    }

    public async Task<UserEntity> EnsureUserAsync(Guid objectId, CancellationToken cancellationToken = default)
    {
        var objectIdString = objectId.ToString();

        var graphUser = await FetchDirectoryUserAsync(objectIdString, cancellationToken);
        if (graphUser == null)
        {
            throw new InvalidOperationException($"Unable to locate Graph user {objectIdString}.");
        }

        var directoryUser = MapGraphUser(graphUser);
        var normalizedEmail = directoryUser.Email?.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.AzureObjectId == objectIdString, cancellationToken);

        if (user == null && !string.IsNullOrWhiteSpace(normalizedEmail))
        {
            user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email != null && u.Email.ToLower() == normalizedEmail,
                    cancellationToken);

            if (user != null && string.IsNullOrWhiteSpace(user.AzureObjectId))
            {
                user.AzureObjectId = objectIdString;
            }
        }

        if (user == null)
        {
            user = new UserEntity
            {
                AzureObjectId = objectIdString,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
        }

        UpdateUserFromDirectoryPayload(user, directoryUser);

        var photo = await TryDownloadPhotoAsync(objectIdString, cancellationToken);
        if (photo != null && photo.Length > 0)
        {
            user.Photo = photo;
            user.PhotoUpdatedAt = DateTime.UtcNow;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<UserEntity?> GetByObjectIdAsync(Guid objectId, CancellationToken cancellationToken = default)
    {
        var id = objectId.ToString();
        return await _context.Users.FirstOrDefaultAsync(u => u.AzureObjectId == id, cancellationToken);
    }

    private async Task<GraphUser?> FetchDirectoryUserAsync(string objectId, CancellationToken cancellationToken)
    {
        try
        {
            return await _graph.Users[objectId].GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = UserSelectFields;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user {ObjectId} from Microsoft Graph", objectId);
            throw;
        }
    }

    private static UserEntity MapGraphUser(GraphUser graphUser)
    {
        var email = !string.IsNullOrWhiteSpace(graphUser.Mail)
            ? graphUser.Mail
            : graphUser.UserPrincipalName ?? string.Empty;
        var friendlyName = GraphUserNameFormatter.FormatFriendlyName(graphUser, email);

        return new UserEntity
        {
            AzureObjectId = graphUser.Id,
            Name = friendlyName,
            FirstName = graphUser.GivenName,
            LastName = graphUser.Surname,
            Email = email,
            UserPrincipalName = graphUser.UserPrincipalName,
            JobTitle = graphUser.JobTitle,
            Role = Compass.Models.UserRole.Visitor
        };
    }

    private void UpdateUserFromDirectoryPayload(UserEntity current, UserEntity payload)
    {
        current.Name = payload.Name;
        current.FirstName = payload.FirstName;
        current.LastName = payload.LastName;
        current.Email = payload.Email;
        current.UserPrincipalName = payload.UserPrincipalName;
        current.JobTitle = payload.JobTitle;
        current.AzureObjectId ??= payload.AzureObjectId;
    }

    private async Task<byte[]?> TryDownloadPhotoAsync(string objectId, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = await _graph.Users[objectId].Photo.Content.GetAsync(cancellationToken: cancellationToken);
            if (stream == null)
            {
                return null;
            }

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream.ToArray();
        }
        catch (ODataError)
        {
            // Graph returns 404 when a user has no photo - treat as best effort.
            return null;
        }
        catch (ServiceException ex)
        {
            _logger.LogWarning(ex, "Unable to download profile photo for {ObjectId}", objectId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading photo for {ObjectId}", objectId);
            return null;
        }
    }
}


