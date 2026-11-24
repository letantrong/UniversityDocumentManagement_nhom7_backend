using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users
{
    public class UserService : IUserService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        public UserService(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }
        public async Task<ApiResponse<PagedResult<UserDto>>> GetPagedUsersAsync(UserFilterRequest request)
        {
            var query = _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsNoTracking();
            // 1. Tìm kiếm theo từ khóa (Email, Tên, Số điện thoại)
            if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
            {
                var keyword = request.SearchKeyword.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(keyword) ||
                                         u.FullName.ToLower().Contains(keyword) ||
                                         (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)));
            }
            // 2. Lọc theo Khoa/Phòng ban
            if (request.DepartmentId.HasValue)
            {
                query = query.Where(u => u.DepartmentId == request.DepartmentId.Value);
            }
            // 3. Lọc theo Vai trò
            if (!string.IsNullOrWhiteSpace(request.RoleName))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == request.RoleName));
            }
            // 4. Lọc theo Trạng thái
            if (request.Status.HasValue)
            {
                query = query.Where(u => u.Status == request.Status.Value);
            }
            var totalCount = await query.CountAsync();
            var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    AvatarUrl = u.AvatarUrl,
                    Status = u.Status,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : null,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
            var pagedResult = new PagedResult<UserDto>(users, totalCount, pageIndex, pageSize);
            return ApiResponse<PagedResult<UserDto>>.SuccessResult(pagedResult, "Lấy danh sách người dùng thành công.");
        }
        public async Task<ApiResponse<UserDto>> GetByIdAsync(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                throw new NotFoundException("Người dùng", id);
            }
            var dto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = user.CreatedAt
            };
            return ApiResponse<UserDto>.SuccessResult(dto, "Lấy thông tin người dùng thành công.");
        }
        public async Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request)
        {
            var email = request.Email.Trim().ToLower();
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
            {
                return ApiResponse<UserDto>.FailureResult($"Email '{request.Email}' đã được đăng ký trong hệ thống.");
            }
            if (request.DepartmentId.HasValue)
            {
                var deptExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value);
                if (!deptExists)
                {
                    return ApiResponse<UserDto>.FailureResult("Khoa/Phòng ban được chọn không tồn tại.");
                }
            }
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = request.FullName.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Status = UserStatus.Active,
                DepartmentId = request.DepartmentId
            };
            // Gán vai trò
            if (request.Roles != null && request.Roles.Any())
            {
                var roles = await _context.Roles
                    .Where(r => request.Roles.Contains(r.Name))
                    .ToListAsync();
                foreach (var role in roles)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            var createdUser = await GetByIdAsync(user.Id);
            return ApiResponse<UserDto>.SuccessResult(createdUser.Data, "Tạo tài khoản người dùng thành công.");
        }
        public async Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                throw new NotFoundException("Người dùng", id);
            }
            if (request.DepartmentId.HasValue && request.DepartmentId != user.DepartmentId)
            {
                var deptExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value);
                if (!deptExists)
                {
                    return ApiResponse<UserDto>.FailureResult("Khoa/Phòng ban mới không tồn tại.");
                }
            }
            user.FullName = request.FullName.Trim();
            user.PhoneNumber = request.PhoneNumber?.Trim();
            user.DepartmentId = request.DepartmentId;
            // Cập nhật lại vai trò
            if (request.Roles != null)
            {
                // Xóa các vai trò cũ
                _context.UserRoles.RemoveRange(user.UserRoles);
                // Thêm các vai trò mới
                var validRoles = await _context.Roles
                    .Where(r => request.Roles.Contains(r.Name))
                    .ToListAsync();
                foreach (var role in validRoles)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }
            await _context.SaveChangesAsync();
            var updatedUser = await GetByIdAsync(user.Id);
            return ApiResponse<UserDto>.SuccessResult(updatedUser.Data, "Cập nhật thông tin người dùng thành công.");
        }
        public async Task<ApiResponse<bool>> ChangeStatusAsync(Guid id, UserStatus newStatus)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new NotFoundException("Người dùng", id);
            }
            user.Status = newStatus;
            if (newStatus == UserStatus.Locked)
            {
                // Nếu khóa tài khoản thì vô hiệu hóa luôn RefreshToken
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
            }
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResult(true, "Cập nhật trạng thái tài khoản thành công.");
        }
        public async Task<ApiResponse<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new NotFoundException("Người dùng", id);
            }
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            // Buộc đăng nhập lại ở các thiết bị
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResult(true, "Đặt lại mật khẩu thành công.");
        }
        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid currentUserId)
        {
            if (id == currentUserId)
            {
                return ApiResponse<bool>.FailureResult("Bạn không thể tự xóa tài khoản của chính mình.");
            }
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                throw new NotFoundException("Người dùng", id);
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResult(true, "Xóa tài khoản người dùng thành công.");
        }
    }
}
