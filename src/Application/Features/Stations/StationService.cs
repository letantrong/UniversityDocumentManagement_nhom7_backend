using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Stations
{
    public class StationService : IStationService
    {
        private readonly IApplicationDbContext _context;
        public StationService(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ApiResponse<List<StationDto>>> GetAllAsync(Guid? departmentId = null, StationStatus? status = null)
        {
            var query = _context.Stations
                .Include(s => s.Department)
                .AsNoTracking();
            if (departmentId.HasValue)
            {
                query = query.Where(s => s.DepartmentId == departmentId.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }
            var stations = await query
                .OrderBy(s => s.Name)
                .Select(s => new StationDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Location = s.Location,
                    Description = s.Description,
                    Status = s.Status,
                    DepartmentId = s.DepartmentId,
                    DepartmentName = s.Department.Name
                })
                .ToListAsync();
            return ApiResponse<List<StationDto>>.SuccessResult(stations, "Lấy danh sách trạm tiếp nhận thành công.");
        }
        public async Task<ApiResponse<StationDto>> GetByIdAsync(Guid id)
        {
            var station = await _context.Stations
                .Include(s => s.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (station == null)
            {
                throw new NotFoundException("Trạm tiếp nhận", id);
            }
            var dto = new StationDto
            {
                Id = station.Id,
                Code = station.Code,
                Name = station.Name,
                Location = station.Location,
                Description = station.Description,
                Status = station.Status,
                DepartmentId = station.DepartmentId,
                DepartmentName = station.Department.Name
            };
            return ApiResponse<StationDto>.SuccessResult(dto, "Lấy thông tin trạm tiếp nhận thành công.");
        }
        public async Task<ApiResponse<StationDto>> CreateAsync(CreateStationRequest request)
        {
            var code = request.Code.Trim().ToUpper();
            if (await _context.Stations.AnyAsync(s => s.Code == code))
            {
                return ApiResponse<StationDto>.FailureResult($"Mã trạm '{code}' đã tồn tại.");
            }
            var department = await _context.Departments.FindAsync(request.DepartmentId);
            if (department == null)
            {
                return ApiResponse<StationDto>.FailureResult("Khoa/Phòng ban được chọn không tồn tại.");
            }
            var station = new DocumentStation
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = request.Name.Trim(),
                Location = request.Location?.Trim(),
                Description = request.Description?.Trim(),
                Status = request.Status,
                DepartmentId = request.DepartmentId
            };
            await _context.Stations.AddAsync(station);
            await _context.SaveChangesAsync();
            var dto = new StationDto
            {
                Id = station.Id,
                Code = station.Code,
                Name = station.Name,
                Location = station.Location,
                Description = station.Description,
                Status = station.Status,
                DepartmentId = station.DepartmentId,
                DepartmentName = department.Name
            };
            return ApiResponse<StationDto>.SuccessResult(dto, "Tạo trạm tiếp nhận thành công.");
        }
        public async Task<ApiResponse<StationDto>> UpdateAsync(Guid id, UpdateStationRequest request)
        {
            var station = await _context.Stations
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (station == null)
            {
                throw new NotFoundException("Trạm tiếp nhận", id);
            }
            if (station.DepartmentId != request.DepartmentId)
            {
                var deptExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId);
                if (!deptExists)
                {
                    return ApiResponse<StationDto>.FailureResult("Khoa/Phòng ban mới không tồn tại.");
                }
                station.DepartmentId = request.DepartmentId;
            }
            station.Name = request.Name.Trim();
            station.Location = request.Location?.Trim();
            station.Description = request.Description?.Trim();
            station.Status = request.Status;
            await _context.SaveChangesAsync();
            var updatedDepartment = await _context.Departments.FindAsync(station.DepartmentId);
            var dto = new StationDto
            {
                Id = station.Id,
                Code = station.Code,
                Name = station.Name,
                Location = station.Location,
                Description = station.Description,
                Status = station.Status,
                DepartmentId = station.DepartmentId,
                DepartmentName = updatedDepartment?.Name ?? string.Empty
            };
            return ApiResponse<StationDto>.SuccessResult(dto, "Cập nhật trạm tiếp nhận thành công.");
        }
        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var station = await _context.Stations.FindAsync(id);
            if (station == null)
            {
                throw new NotFoundException("Trạm tiếp nhận", id);
            }
            _context.Stations.Remove(station);
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResult(true, "Xóa trạm tiếp nhận thành công.");
        }
    }
}
