using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Document : BaseEntity
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Abstract { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public DocumentSecurityLevel SecurityLevel { get; set; } = DocumentSecurityLevel.Internal;
    public int CurrentVersionNumber { get; set; } = 1;

    public Guid CategoryId { get; set; }
    public DocumentCategory Category { get; set; } = null!;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public Guid? FolderId { get; set; }
    public Folder? Folder { get; set; }

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<IntakeRecord> IntakeRecords { get; set; } = new List<IntakeRecord>();
}
