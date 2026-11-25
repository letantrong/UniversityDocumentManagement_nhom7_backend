using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Roles;

public record RoleDto(Guid Id, string Name, string Description, bool IsSystemRole, IReadOnlyList<string> Permissions);

public record GetRolesQuery : IRequest<Result<List<RoleDto>>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetRolesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                r.RolePermissions.Select(rp => rp.Permission.Code).ToList()))
            .ToListAsync(cancellationToken);

        return Result<List<RoleDto>>.Success(roles);
    }
}
