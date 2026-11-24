using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
    public enum StationStatus
    {
        Inactive = 0,      // Ngừng hoạt động
        Active = 1,        // Đang hoạt động / sẵn sàng nhận tài liệu
        Maintenance = 2    // Đang bảo trì
    }
}
