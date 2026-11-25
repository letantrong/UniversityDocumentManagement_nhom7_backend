using Domain.Enums;

namespace Application.Features.Documents.DTOs;

public record DocumentVersionDto(
    Guid Id,
    int VersionNumber,
    string FileName,
    string StoredFileName,
    long FileSize,
    string MimeType,
    string? FileHash,
    string? ChangeSummary,
    Guid UploadedById,
    string UploadedByName,
    DateTime CreatedAt);

public record DocumentDto(
    Guid Id,
    string DocumentNumber,
    string Title,
    string Description,
    string? Abstract,
    DocumentStatus Status,
    string StatusName,
    DocumentSecurityLevel SecurityLevel,
    string SecurityLevelName,
    int CurrentVersionNumber,
    Guid CategoryId,
    string CategoryName,
    Guid DepartmentId,
    string DepartmentName,
    Guid? FolderId,
    string? FolderName,
    Guid OwnerId,
    string OwnerName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<DocumentVersionDto> Versions);

public record CreateDocumentDto(
    string Title,
    string Description,
    string? Abstract,
    DocumentSecurityLevel SecurityLevel,
    Guid CategoryId,
    Guid DepartmentId,
    Guid? FolderId);

public record UpdateDocumentDto(
    string Title,
    string Description,
    string? Abstract,
    DocumentSecurityLevel SecurityLevel,
    Guid CategoryId,
    Guid? FolderId,
    DocumentStatus? Status);
