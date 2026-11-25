using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth;

// DTOs
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId,
    string Username,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public record LoginDto(string UsernameOrEmail, string Password);
public record RegisterDto(string Username, string Email, string Password, string FullName, string? PhoneNumber, Guid? DepartmentId);
public record RefreshTokenRequestDto(string AccessToken, string RefreshToken);

// Commands
public record LoginCommand(string UsernameOrEmail, string Password) : IRequest<Result<AuthResponseDto>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty().WithMessage("Username or Email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeService _dateTimeService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => (u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail) && !u.IsDeleted, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<AuthResponseDto>.Failure("Invalid username/email or password.");
        }

        if (!user.IsActive)
        {
            return Result<AuthResponseDto>.Failure("User account is deactivated.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _dateTimeService.UtcNow.AddDays(7),
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new AuthResponseDto(
            accessToken,
            refreshTokenValue,
            _dateTimeService.UtcNow.AddHours(2),
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            roles,
            permissions);

        return Result<AuthResponseDto>.Success(response, "Login successful.");
    }
}

public record RegisterCommand(string Username, string Email, string Password, string FullName, string? PhoneNumber, Guid? DepartmentId) : IRequest<Result<Guid>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeService _dateTimeService;

    public RegisterCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IDateTimeService dateTimeService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users
            .AnyAsync(u => (u.Username == request.Username || u.Email == request.Email) && !u.IsDeleted, cancellationToken);

        if (existingUser)
        {
            return Result<Guid>.Failure("Username or Email already exists.");
        }

        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

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

        if (defaultRole != null)
        {
            user.UserRoles.Add(new UserRole { RoleId = defaultRole.Id, UserId = user.Id });
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id, "User registered successfully.");
    }
}

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<Result<AuthResponseDto>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeService _dateTimeService;

    public RefreshTokenCommandHandler(IApplicationDbContext context, IJwtTokenService jwtTokenService, IDateTimeService dateTimeService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return Result<AuthResponseDto>.Failure("Invalid access token.");

        var storedRefreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsDeleted, cancellationToken);

        if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.");

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = _dateTimeService.UtcNow;

        var user = storedRefreshToken.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = _dateTimeService.UtcNow.AddDays(7),
            CreatedAt = _dateTimeService.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        var response = new AuthResponseDto(
            newAccessToken,
            newRefreshToken,
            _dateTimeService.UtcNow.AddHours(2),
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            roles,
            permissions);

        return Result<AuthResponseDto>.Success(response, "Token refreshed successfully.");
    }
}
