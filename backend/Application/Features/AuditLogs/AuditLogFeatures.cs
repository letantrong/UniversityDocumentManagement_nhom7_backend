using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AuditLogs;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    AuditAction Action,
    string ActionName,
    string EntityName,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTime CreatedAt);

public record GetAuditLogsQuery(PaginationParams Params, AuditAction? Action = null) : IRequest<Result<PaginatedList<AuditLogDto>>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PaginatedList<AuditLogDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs
            .Where(a => !a.IsDeleted)
            .AsNoTracking();

        if (request.Action.HasValue)
            query = query.Where(a => a.Action == request.Action.Value);

        if (!string.IsNullOrWhiteSpace(request.Params.SearchTerm))
        {
            var term = request.Params.SearchTerm.Trim().ToLower();
            query = query.Where(a => (a.UserName != null && a.UserName.ToLower().Contains(term)) ||
                                     a.EntityName.ToLower().Contains(term) ||
                                     (a.EntityId != null && a.EntityId.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Params.PageNumber - 1) * request.Params.PageSize)
            .Take(request.Params.PageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.UserId,
                a.UserName,
                a.Action,
                a.Action.ToString(),
                a.EntityName,
                a.EntityId,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<AuditLogDto>>.Success(
            new PaginatedList<AuditLogDto>(items, totalCount, request.Params.PageNumber, request.Params.PageSize));
    }
}
