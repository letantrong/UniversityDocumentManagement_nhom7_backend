using System;
using System.Collections.Generic;
using System.Text;
using Application.Common.Interfaces;

namespace Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // Mã hóa một chiều an toàn bằng BCrypt (work factor mặc định = 11)
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        public bool VerifyPassword(string password, string passwordHash)
        {
            // So khớp mật khẩu nhập vào với mã hash đã lưu
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
