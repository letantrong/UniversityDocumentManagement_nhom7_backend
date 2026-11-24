using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
    public enum UserStatus
    {
        Inactive = 0,   // Chưa kích hoạt
        Active = 1,     // Đang hoạt động bình thường
        Locked = 2      // Bị khóa tài khoản
    }
}
