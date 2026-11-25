using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Permissions;

public enum DocumentPermissionAction
{
    View,
    Download,
    Edit,
    Delete,
    Share
}

public record DocumentAccessMatrixDto(
    Guid DocumentId,
    bool CanView,
    bool CanDownload,
    bool CanEdit,
    bool CanDelete,
    bool CanShare);

public record PermissionDto(Guid Id, string Code, string Name, string Description, string Module);

public record GetPermissionsQuery : IRequest<Result<List<PermissionDto>>>;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, Result<List<PermissionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPermissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PermissionDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Description, p.Module))
            .ToListAsync(cancellationToken);

        return Result<List<PermissionDto>>.Success(permissions);
    }
}

public record GetDocumentAccessQuery(Guid DocumentId) : IRequest<Result<DocumentAccessMatrixDto>>;

public class GetDocumentAccessQueryHandler : IRequestHandler<GetDocumentAccessQuery, Result<DocumentAccessMatrixDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentAccessQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<DocumentAccessMatrixDto>> Handle(
        GetDocumentAccessQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result<DocumentAccessMatrixDto>.Failure("Unauthorized");

        var isPrivileged = _currentUserService.Roles.Any(r =>
            r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Manager", StringComparison.OrdinalIgnoreCase));

        var matrix = new DocumentAccessMatrixDto(
            DocumentId: request.DocumentId,
            CanView: true,
            CanDownload: isPrivileged,
            CanEdit: isPrivileged,
            CanDelete: isPrivileged,
            CanShare: isPrivileged);

        return Result<DocumentAccessMatrixDto>.Success(matrix);
    }
}

public record CheckDocumentPermissionQuery(Guid DocumentId, DocumentPermissionAction Action)
    : IRequest<Result<bool>>;

public class CheckDocumentPermissionQueryHandler
    : IRequestHandler<CheckDocumentPermissionQuery, Result<bool>>
{
    private readonly ISender _sender;

    public CheckDocumentPermissionQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<Result<bool>> Handle(
        CheckDocumentPermissionQuery request,
        CancellationToken cancellationToken)
    {
        var accessResult = await _sender.Send(
            new GetDocumentAccessQuery(request.DocumentId),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Data is null)
            return Result<bool>.Failure(accessResult.Message ?? "Unable to evaluate document permissions.");

        var allowed = request.Action switch
        {
            DocumentPermissionAction.View => accessResult.Data.CanView,
            DocumentPermissionAction.Download => accessResult.Data.CanDownload,
            DocumentPermissionAction.Edit => accessResult.Data.CanEdit,
            DocumentPermissionAction.Delete => accessResult.Data.CanDelete,
            DocumentPermissionAction.Share => accessResult.Data.CanShare,
            _ => false
        };

        return Result<bool>.Success(allowed);
    }
}