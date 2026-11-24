using Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Roles
{
    public interface IRoleService
    {
        Task<ApiResponse<List<RoleDto>>> GetAllAsync();
    }
}
