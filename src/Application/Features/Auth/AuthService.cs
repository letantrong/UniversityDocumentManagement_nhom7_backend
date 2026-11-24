using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IConfiguration _configuration;
        public AuthService(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _configuration = configuration;
        }
        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            // 1. Tìm User theo Email cùng Khoa và Roles
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());
            if (user == null)
            {
                return ApiResponse<LoginResponse>.FailureResult("Email hoặc mật khẩu không chính xác.");
            }
            // 2. Kiểm tra mật khẩu bằng PasswordHasher (BCrypt)
            var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return ApiResponse<LoginResponse>.FailureResult("Email hoặc mật khẩu không chính xác.");
            }
            // 3. Kiểm tra trạng thái tài khoản
            if (user.Status == UserStatus.Locked)
            {
                return ApiResponse<LoginResponse>.FailureResult("Tài khoản đã bị khóa. Vui lòng liên hệ Quản trị viên.");
            }
            if (user.Status == UserStatus.Inactive)
            {
                return ApiResponse<LoginResponse>.FailureResult("Tài khoản chưa được kích hoạt.");
            }
            // 4. Lấy danh sách Roles của User
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            // 5. Phát hành Access Token (JWT) và Refresh Token
            var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user, roles);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
            var refreshDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDays);
            await _context.SaveChangesAsync();
            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    AvatarUrl = user.AvatarUrl,
                    DepartmentId = user.DepartmentId,
                    DepartmentName = user.Department?.Name,
                    Roles = roles
                }
            };
            return ApiResponse<LoginResponse>.SuccessResult(response, "Đăng nhập thành công.");
        }
        public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                return ApiResponse<LoginResponse>.FailureResult("Access Token không hợp lệ.");
            }
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return ApiResponse<LoginResponse>.FailureResult("Token không hợp lệ.");
            }
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return ApiResponse<LoginResponse>.FailureResult("Refresh Token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
            }
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var (newAccessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user, roles);
            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
            var refreshDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDays);
            await _context.SaveChangesAsync();
            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    AvatarUrl = user.AvatarUrl,
                    DepartmentId = user.DepartmentId,
                    DepartmentName = user.Department?.Name,
                    Roles = roles
                }
            };
            return ApiResponse<LoginResponse>.SuccessResult(response, "Làm mới Token thành công.");
        }
        public async Task<ApiResponse<bool>> LogoutAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
            }
            return ApiResponse<bool>.SuccessResult(true, "Đăng xuất thành công.");
        }
        public async Task<ApiResponse<UserInfoDto>> GetCurrentUserAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new NotFoundException("Người dùng", userId);
            }
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var userInfo = new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                Roles = roles
            };
            return ApiResponse<UserInfoDto>.SuccessResult(userInfo, "Lấy thông tin người dùng thành công.");
        }
    }
}
