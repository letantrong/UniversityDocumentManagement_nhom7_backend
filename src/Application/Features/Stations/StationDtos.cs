using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Stations
{
    public class StationDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public StationStatus Status { get; set; }
        public string StatusName => Status switch
        {
            StationStatus.Active => "Hoạt động",
            StationStatus.Maintenance => "Bảo trì",
            _ => "Ngừng hoạt động"
        };
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }
    public class CreateStationRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public Guid DepartmentId { get; set; }
        public StationStatus Status { get; set; } = StationStatus.Active;
    }
    public class UpdateStationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public Guid DepartmentId { get; set; }
        public StationStatus Status { get; set; } = StationStatus.Active;
    }
}
