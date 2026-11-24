using Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Department : AuditableEntity<Guid>
    {
        public string Code { get; set; } = string.Empty;          // Mã khoa/phòng ban, ví dụ: "IT", "DAOTAO"
        public string Name { get; set; } = string.Empty;          // Tên đầy đủ
        public string? Description { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<DocumentStation> Stations { get; set; } = new List<DocumentStation>();
    }

}
