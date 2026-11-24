using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Users
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public UserStatus Status { get; set; }
        public string StatusName => Status switch
        {
            UserStatus.Active => "Hoạt động",
            UserStatus.Locked => "Đã khóa",
            _ => "Chưa kích hoạt"
        };
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Guid? DepartmentId { get; set; }
        public List<string> Roles { get; set; } = new();
    }
    public class UpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Guid? DepartmentId { get; set; }
        public List<string> Roles { get; set; } = new();
    }
    public class ChangeUserStatusRequest
    {
        public UserStatus Status { get; set; }
    }
    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
    public class UserFilterRequest
    {
        public string? SearchKeyword { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? RoleName { get; set; }
        public UserStatus? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
