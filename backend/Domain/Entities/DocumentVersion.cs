using Domain.Common;

namespace Domain.Entities;

public class DocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? FileHash { get; set; }
    public string? ChangeSummary { get; set; }

    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}
