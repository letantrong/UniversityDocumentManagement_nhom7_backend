using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Departments
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IApplicationDbContext _context;
        public DepartmentService(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ApiResponse<List<DepartmentDto>>> GetAllAsync(bool? isActive = null)
        {
            var query = _context.Departments
                .Include(d => d.Users)
                .Include(d => d.Stations)
                .AsNoTracking();
            if (isActive.HasValue)
            {
                query = query.Where(d => d.IsActive == isActive.Value);
            }
            var departments = await query
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    Email = d.Email,
                    PhoneNumber = d.PhoneNumber,
                    IsActive = d.IsActive,
                    UserCount = d.Users.Count,
                    StationCount = d.Stations.Count
                })
                .ToListAsync();
            return ApiResponse<List<DepartmentDto>>.SuccessResult(departments, "Lấy danh sách khoa/phòng ban thành công.");
        }
        public async Task<ApiResponse<DepartmentDto>> GetByIdAsync(Guid id)
        {
            var dept = await _context.Departments
                .Include(d => d.Users)
                .Include(d => d.Stations)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dept == null)
            {
                throw new NotFoundException("Khoa/Phòng ban", id);
            }
            var dto = new DepartmentDto
            {
                Id = dept.Id,
                Code = dept.Code,
                Name = dept.Name,
                Description = dept.Description,
                Email = dept.Email,
                PhoneNumber = dept.PhoneNumber,
                IsActive = dept.IsActive,
                UserCount = dept.Users.Count,
                StationCount = dept.Stations.Count
            };
            return ApiResponse<DepartmentDto>.SuccessResult(dto, "Lấy thông tin khoa/phòng ban thành công.");
        }
        public async Task<ApiResponse<DepartmentDto>> CreateAsync(CreateDepartmentRequest request)
        {
            var code = request.Code.Trim().ToUpper();
            if (await _context.Departments.AnyAsync(d => d.Code == code))
            {
                return ApiResponse<DepartmentDto>.FailureResult($"Mã khoa/phòng ban '{code}' đã tồn tại.");
            }
            var department = new Department
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Email = request.Email?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                IsActive = true
            };
            await _context.Departments.AddAsync(department);
            await _context.SaveChangesAsync();
            var dto = new DepartmentDto
            {
                Id = department.Id,
                Code = department.Code,
                Name = department.Name,
                Description = department.Description,
                Email = department.Email,
                PhoneNumber = department.PhoneNumber,
                IsActive = department.IsActive,
                UserCount = 0,
                StationCount = 0
            };
            return ApiResponse<DepartmentDto>.SuccessResult(dto, "Tạo khoa/phòng ban thành công.");
        }
        public async Task<ApiResponse<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                throw new NotFoundException("Khoa/Phòng ban", id);
            }
            department.Name = request.Name.Trim();
            department.Description = request.Description?.Trim();
            department.Email = request.Email?.Trim();
            department.PhoneNumber = request.PhoneNumber?.Trim();
            department.IsActive = request.IsActive;
            await _context.SaveChangesAsync();
            var userCount = await _context.Users.CountAsync(u => u.DepartmentId == id);
            var stationCount = await _context.Stations.CountAsync(s => s.DepartmentId == id);
            var dto = new DepartmentDto
            {
                Id = department.Id,
                Code = department.Code,
                Name = department.Name,
                Description = department.Description,
                Email = department.Email,
                PhoneNumber = department.PhoneNumber,
                IsActive = department.IsActive,
                UserCount = userCount,
                StationCount = stationCount
            };
            return ApiResponse<DepartmentDto>.SuccessResult(dto, "Cập nhật khoa/phòng ban thành công.");
        }
        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var department = await _context.Departments
                .Include(d => d.Users)
                .Include(d => d.Stations)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (department == null)
            {
                throw new NotFoundException("Khoa/Phòng ban", id);
            }
            if (department.Users.Any() || department.Stations.Any())
            {
                return ApiResponse<bool>.FailureResult("Không thể xóa khoa/phòng ban đang có người dùng hoặc trạm tiếp nhận liên kết. Bạn có thể chọn vô hiệu hóa thay vì xóa.");
            }
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResult(true, "Xóa khoa/phòng ban thành công.");
        }
    }
}
