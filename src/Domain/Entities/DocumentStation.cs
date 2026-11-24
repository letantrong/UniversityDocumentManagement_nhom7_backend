using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class DocumentStation : AuditableEntity<Guid>
    {
        public string Code { get; set; } = string.Empty;         // Mã trạm: "STATION_01"
        public string Name { get; set; } = string.Empty;         // Tên trạm: "Trạm tiếp nhận tầng 1"
        public string? Location { get; set; }                     // Vị trí: "Phòng Quản lý Đào tạo"
        public string? Description { get; set; }
        public StationStatus Status { get; set; } = StationStatus.Active;
        // Trạm này thuộc khoa/phòng ban nào quản lý
        public Guid DepartmentId { get; set; }
        public virtual Department Department { get; set; } = null!;
    }
}
