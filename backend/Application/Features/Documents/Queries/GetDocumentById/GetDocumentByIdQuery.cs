using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Documents.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Documents.Queries.GetDocumentById;

public record GetDocumentByIdQuery(Guid Id) : IRequest<Result<DocumentDto>>;

public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, Result<DocumentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDocumentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DocumentDto>> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var doc = await _context.Documents
            .Include(d => d.Category)
            .Include(d => d.Department)
            .Include(d => d.Folder)
            .Include(d => d.Owner)
            .Include(d => d.Versions.Where(v => !v.IsDeleted))
                .ThenInclude(v => v.UploadedBy)
            .Where(d => d.Id == request.Id && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
            return Result<DocumentDto>.Failure("Document not found.");

        var versions = doc.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentVersionDto(
                v.Id,
                v.VersionNumber,
                v.FileName,
                v.StoredFileName,
                v.FileSize,
                v.MimeType,
                v.FileHash,
                v.ChangeSummary,
                v.UploadedById,
                v.UploadedBy.FullName,
                v.CreatedAt))
            .ToList();

        var dto = new DocumentDto(
            doc.Id,
            doc.DocumentNumber,
            doc.Title,
            doc.Description,
            doc.Abstract,
            doc.Status,
            doc.Status.ToString(),
            doc.SecurityLevel,
            doc.SecurityLevel.ToString(),
            doc.CurrentVersionNumber,
            doc.CategoryId,
            doc.Category.Name,
            doc.DepartmentId,
            doc.Department.Name,
            doc.FolderId,
            doc.Folder != null ? doc.Folder.Name : null,
            doc.OwnerId,
            doc.Owner.FullName,
            doc.CreatedAt,
            doc.UpdatedAt,
            versions);

        return Result<DocumentDto>.Success(dto);
    }
}
