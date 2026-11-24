using Application.Common.Models;
using Application.Features.Stations;
using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/v1/stations")]
[Authorize]
public class StationsController : ControllerBase
{
    private readonly IStationService _stationService;

    public StationsController(IStationService stationService)
    {
        _stationService = stationService;
    }

    /// <summary>
    /// Lấy danh sách trạm tiếp nhận (có thể lọc theo Khoa hoặc Trạng thái)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? departmentId, [FromQuery] StationStatus? status)
    {
        var result = await _stationService.GetAllAsync(departmentId, status);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết một trạm tiếp nhận theo Id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _stationService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới trạm tiếp nhận tài liệu (ADMIN hoặc Trưởng phòng ban)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.DepartmentHead}")]
    [ProducesResponseType(typeof(ApiResponse<StationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStationRequest request)
    {
        var result = await _stationService.CreateAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật trạm tiếp nhận (ADMIN hoặc Trưởng phòng ban)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.DepartmentHead}")]
    [ProducesResponseType(typeof(ApiResponse<StationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStationRequest request)
    {
        var result = await _stationService.UpdateAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Xóa trạm tiếp nhận (Chỉ dành cho ADMIN)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _stationService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}