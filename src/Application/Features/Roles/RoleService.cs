using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Roles
{
    public class RoleService : IRoleService
    {
        private readonly IApplicationDbContext _context;
        public RoleService(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ApiResponse<List<RoleDto>>> GetAllAsync()
        {
            var roles = await _context.Roles
                .Include(r => r.UserRoles)
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    NormalizedName = r.NormalizedName,
                    Description = r.Description,
                    UserCount = r.UserRoles.Count
                })
                .ToListAsync();
            return ApiResponse<List<RoleDto>>.SuccessResult(roles, "Lấy danh sách vai trò thành công.");
        }
    }
}
