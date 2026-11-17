using Domain.Common;

namespace Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Document> OwnedDocuments { get; set; } = new List<Document>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<IntakeRecord> HandledIntakes { get; set; } = new List<IntakeRecord>();
}
