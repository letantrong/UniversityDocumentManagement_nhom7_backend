using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Documents.DTOs;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Documents.Queries.GetDocuments;

public record GetDocumentsQuery(
    PaginationParams Params,
    Guid? CategoryId = null,
    Guid? DepartmentId = null,
    Guid? FolderId = null,
    DocumentStatus? Status = null) : IRequest<Result<PaginatedList<DocumentDto>>>;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, Result<PaginatedList<DocumentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDocumentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<DocumentDto>>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Documents
            .Include(d => d.Category)
            .Include(d => d.Department)
            .Include(d => d.Folder)
            .Include(d => d.Owner)
            .Include(d => d.Versions.Where(v => !v.IsDeleted))
                .ThenInclude(v => v.UploadedBy)
            .Where(d => !d.IsDeleted)
            .AsNoTracking();

        if (request.CategoryId.HasValue)
            query = query.Where(d => d.CategoryId == request.CategoryId.Value);

        if (request.DepartmentId.HasValue)
            query = query.Where(d => d.DepartmentId == request.DepartmentId.Value);

        if (request.FolderId.HasValue)
            query = query.Where(d => d.FolderId == request.FolderId.Value);

        if (request.Status.HasValue)
            query = query.Where(d => d.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Params.SearchTerm))
        {
            var term = request.Params.SearchTerm.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(term) ||
                                     d.DocumentNumber.ToLower().Contains(term) ||
                                     d.Description.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Params.PageNumber - 1) * request.Params.PageSize)
            .Take(request.Params.PageSize)
            .Select(doc => new DocumentDto(
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
                doc.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new DocumentVersionDto(
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
                    v.CreatedAt)).ToList()))
            .ToListAsync(cancellationToken);

        var result = new PaginatedList<DocumentDto>(items, totalCount, request.Params.PageNumber, request.Params.PageSize);
        return Result<PaginatedList<DocumentDto>>.Success(result);
    }
}
