using Application.Common.Models;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Users
{
    public interface IUserService
    {
        Task<ApiResponse<PagedResult<UserDto>>> GetPagedUsersAsync(UserFilterRequest request);
        Task<ApiResponse<UserDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request);
        Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request);
        Task<ApiResponse<bool>> ChangeStatusAsync(Guid id, UserStatus newStatus);
        Task<ApiResponse<bool>> ResetPasswordAsync(Guid id, string newPassword);
        Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid currentUserId);
    }
}
