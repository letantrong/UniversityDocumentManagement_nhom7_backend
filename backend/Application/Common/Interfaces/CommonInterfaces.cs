using System.Security.Claims;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}

public interface IDateTimeService
{
    DateTime UtcNow { get; }
}

public interface IFileStorageService
{
    Task<(string StoredFileName, string FilePath, long FileSize, string MimeType, string FileHash)> SaveFileAsync(
        Stream stream, string originalFileName, string contentType, CancellationToken cancellationToken = default);

    Task<(Stream FileStream, string ContentType, string FileName)> GetFileAsync(
        string storedFileName, CancellationToken cancellationToken = default);

    Task<bool> DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default);

    bool IsAllowedExtension(string fileName);
    bool IsAllowedMimeType(string mimeType);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public interface INotificationService
{
    Task SendToUserAsync(Guid userId, string title, string message, string? referenceId = null, CancellationToken cancellationToken = default);
    Task SendToRoleAsync(string roleName, string title, string message, string? referenceId = null, CancellationToken cancellationToken = default);
    Task BroadcastAsync(string title, string message, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
