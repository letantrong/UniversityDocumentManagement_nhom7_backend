using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Documents.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Documents.Queries.SearchDocuments;

public record SearchDocumentsQuery(string Keyword, int Limit = 10) : IRequest<Result<List<DocumentDto>>>;

public class SearchDocumentsQueryHandler : IRequestHandler<SearchDocumentsQuery, Result<List<DocumentDto>>>
{
    private readonly IApplicationDbContext _context;

    public SearchDocumentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<DocumentDto>>> Handle(SearchDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return Result<List<DocumentDto>>.Success(new List<DocumentDto>());

        var term = request.Keyword.Trim().ToLower();

        var docs = await _context.Documents
            .Include(d => d.Category)
            .Include(d => d.Department)
            .Include(d => d.Folder)
            .Include(d => d.Owner)
            .Where(d => !d.IsDeleted && (
                d.Title.ToLower().Contains(term) ||
                d.DocumentNumber.ToLower().Contains(term) ||
                d.Description.ToLower().Contains(term)))
            .OrderByDescending(d => d.CreatedAt)
            .Take(request.Limit > 50 ? 50 : request.Limit)
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
                new List<DocumentVersionDto>()))
            .ToListAsync(cancellationToken);

        return Result<List<DocumentDto>>.Success(docs);
    }
}
