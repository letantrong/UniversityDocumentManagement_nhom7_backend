using Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Auth
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ApiResponse<bool>> LogoutAsync(Guid userId);
        Task<ApiResponse<UserInfoDto>> GetCurrentUserAsync(Guid userId);
    }
}
