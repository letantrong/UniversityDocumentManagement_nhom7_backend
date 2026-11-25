using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Documents.Commands.CreateDocument;

public record CreateDocumentCommand(
    string Title,
    string Description,
    string? Abstract,
    DocumentSecurityLevel SecurityLevel,
    Guid CategoryId,
    Guid DepartmentId,
    Guid? FolderId,
    string? FileName = null,
    Stream? FileStream = null,
    string? ContentType = null) : IRequest<Result<Guid>>;

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IFileStorageService _fileStorageService;

    public CreateDocumentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<Guid>> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);
        if (category == null)
            return Result<Guid>.Failure("Category not found.");

        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId && !d.IsDeleted, cancellationToken);
        if (department == null)
            return Result<Guid>.Failure("Department not found.");

        var ownerId = _currentUserService.UserId ?? (await _context.Users.Select(u => u.Id).FirstOrDefaultAsync(cancellationToken));

        var currentYear = _dateTimeService.UtcNow.Year;
        var countThisYear = await _context.Documents.CountAsync(d => d.CreatedAt.Year == currentYear, cancellationToken);
        var documentNumber = DocumentNumber.Create(department.Code, currentYear, countThisYear + 1);

        var document = new Document
        {
            DocumentNumber = documentNumber.Value,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Abstract = request.Abstract,
            SecurityLevel = request.SecurityLevel,
            Status = DocumentStatus.Draft,
            CurrentVersionNumber = 1,
            CategoryId = request.CategoryId,
            DepartmentId = request.DepartmentId,
            FolderId = request.FolderId,
            OwnerId = ownerId,
            CreatedAt = _dateTimeService.UtcNow
        };

        if (request.FileStream != null && !string.IsNullOrWhiteSpace(request.FileName))
        {
            var (storedFileName, filePath, fileSize, mimeType, fileHash) = await _fileStorageService.SaveFileAsync(
                request.FileStream, request.FileName, request.ContentType ?? "application/octet-stream", cancellationToken);

            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = 1,
                FileName = request.FileName,
                StoredFileName = storedFileName,
                FilePath = filePath,
                FileSize = fileSize,
                MimeType = mimeType,
                FileHash = fileHash,
                ChangeSummary = "Initial version uploaded.",
                UploadedById = ownerId,
                CreatedAt = _dateTimeService.UtcNow
            };

            document.Versions.Add(version);
        }

        _context.Documents.Add(document);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            UserName = _currentUserService.Username,
            Action = AuditAction.Create,
            EntityName = nameof(Document),
            EntityId = document.Id.ToString(),
            NewValues = $"DocumentNumber: {document.DocumentNumber}, Title: {document.Title}",
            CreatedAt = _dateTimeService.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(document.Id, "Document created successfully.");
    }
}
