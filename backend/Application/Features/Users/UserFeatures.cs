using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    bool IsActive,
    Guid? DepartmentId,
    string? DepartmentName,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);

public record GetUsersQuery(PaginationParams Params) : IRequest<Result<PaginatedList<UserDto>>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PaginatedList<UserDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Params.SearchTerm))
        {
            var term = request.Params.SearchTerm.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(term) ||
                                     u.Email.ToLower().Contains(term) ||
                                     u.FullName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Params.PageNumber - 1) * request.Params.PageSize)
            .Take(request.Params.PageSize)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.AvatarUrl,
                u.IsActive,
                u.DepartmentId,
                u.Department != null ? u.Department.Name : null,
                u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        var paginated = new PaginatedList<UserDto>(items, totalCount, request.Params.PageNumber, request.Params.PageSize);
        return Result<PaginatedList<UserDto>>.Success(paginated);
    }
}

public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.Id == request.Id && !u.IsDeleted)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.AvatarUrl,
                u.IsActive,
                u.DepartmentId,
                u.Department != null ? u.Department.Name : null,
                u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return Result<UserDto>.Failure("User not found.");

        return Result<UserDto>.Success(user);
    }
}

public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    Guid? DepartmentId,
    List<Guid>? RoleIds) : IRequest<Result<Guid>>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeService _dateTimeService;

    public CreateUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IDateTimeService dateTimeService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Users.AnyAsync(u => (u.Username == request.Username || u.Email == request.Email) && !u.IsDeleted, cancellationToken);
        if (exists)
            return Result<Guid>.Failure("User with this username or email already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = _dateTimeService.UtcNow
        };

        if (request.RoleIds != null && request.RoleIds.Count > 0)
        {
            foreach (var roleId in request.RoleIds)
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id, "User created successfully.");
    }
}
