using Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Departments
{
    public interface IDepartmentService
    {
        Task<ApiResponse<List<DepartmentDto>>> GetAllAsync(bool? isActive = null);
        Task<ApiResponse<DepartmentDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<DepartmentDto>> CreateAsync(CreateDepartmentRequest request);
        Task<ApiResponse<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
