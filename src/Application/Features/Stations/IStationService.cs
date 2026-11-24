using Application.Common.Models;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Stations
{
    public interface IStationService
    {
        Task<ApiResponse<List<StationDto>>> GetAllAsync(Guid? departmentId = null, StationStatus? status = null);
        Task<ApiResponse<StationDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<StationDto>> CreateAsync(CreateStationRequest request);
        Task<ApiResponse<StationDto>> UpdateAsync(Guid id, UpdateStationRequest request);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
