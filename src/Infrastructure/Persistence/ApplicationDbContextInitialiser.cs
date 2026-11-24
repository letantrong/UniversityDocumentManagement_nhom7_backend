using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Common.Interfaces;
using Domain.Common;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContextInitialiser
    {
        private readonly ILogger<ApplicationDbContextInitialiser> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        public ApplicationDbContextInitialiser(
            ILogger<ApplicationDbContextInitialiser> logger,
            ApplicationDbContext context,
            IPasswordHasher passwordHasher)
        {
            _logger = logger;
            _context = context;
            _passwordHasher = passwordHasher;
        }
        public async Task InitialiseAsync()
        {
            try
            {
                if (_context.Database.IsSqlServer())
                {
                    await _context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đã xảy ra lỗi khi chạy migration cơ sở dữ liệu.");
                throw;
            }
        }
        public async Task SeedAsync()
        {
            try
            {
                await TrySeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đã xảy ra lỗi khi tạo dữ liệu mẫu (Seed Data).");
                throw;
            }
        }
        private async Task TrySeedAsync()
        {
            // 1. Khởi tạo 5 Roles mặc định
            var defaultRoles = new List<Role>
        {
            new() { Id = Guid.NewGuid(), Name = SystemRoles.Admin, NormalizedName = SystemRoles.Admin.ToUpper(), Description = "Quản trị viên toàn hệ thống" },
            new() { Id = Guid.NewGuid(), Name = SystemRoles.DepartmentHead, NormalizedName = SystemRoles.DepartmentHead.ToUpper(), Description = "Trưởng khoa / Trưởng phòng ban" },
            new() { Id = Guid.NewGuid(), Name = SystemRoles.DepartmentStaff, NormalizedName = SystemRoles.DepartmentStaff.ToUpper(), Description = "Cán bộ / Nhân viên khoa phòng" },
            new() { Id = Guid.NewGuid(), Name = SystemRoles.User, NormalizedName = SystemRoles.User.ToUpper(), Description = "Người dùng tra cứu tài liệu" },
            new() { Id = Guid.NewGuid(), Name = SystemRoles.Guest, NormalizedName = SystemRoles.Guest.ToUpper(), Description = "Khách tiếp nhận qua mã QR" }
        };
            foreach (var role in defaultRoles)
            {
                if (!await _context.Roles.AnyAsync(r => r.Name == role.Name))
                {
                    await _context.Roles.AddAsync(role);
                }
            }
            await _context.SaveChangesAsync();
            // 2. Khởi tạo các Phòng ban mẫu
            var itDept = await _context.Departments.FirstOrDefaultAsync(d => d.Code == "CNTT");
            if (itDept == null)
            {
                itDept = new Department
                {
                    Id = Guid.NewGuid(),
                    Code = "CNTT",
                    Name = "Khoa Công nghệ Thông tin",
                    Description = "Quản lý các tài liệu học phần và văn bản khoa CNTT",
                    Email = "cntt@ucdms.edu.vn",
                    PhoneNumber = "0281234567",
                    IsActive = true
                };
                await _context.Departments.AddAsync(itDept);
            }
            var academicDept = await _context.Departments.FirstOrDefaultAsync(d => d.Code == "QLDT");
            if (academicDept == null)
            {
                academicDept = new Department
                {
                    Id = Guid.NewGuid(),
                    Code = "QLDT",
                    Name = "Phòng Quản lý Đào tạo",
                    Description = "Tiếp nhận và quản lý hồ sơ, tài liệu đào tạo toàn trường",
                    Email = "qldt@ucdms.edu.vn",
                    PhoneNumber = "0287654321",
                    IsActive = true
                };
                await _context.Departments.AddAsync(academicDept);
            }
            await _context.SaveChangesAsync();
            // 3. Khởi tạo Tài khoản Admin mặc định
            var adminEmail = "admin@ucdms.edu.vn";
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail,
                    FullName = "System Administrator",
                    PhoneNumber = "0901234567",
                    PasswordHash = _passwordHasher.HashPassword("Admin@123456"),
                    Status = UserStatus.Active,
                    DepartmentId = itDept?.Id
                };
                await _context.Users.AddAsync(adminUser);
                await _context.SaveChangesAsync();
                // Gán Role ADMIN cho tài khoản này
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == SystemRoles.Admin);
                if (adminRole != null)
                {
                    await _context.UserRoles.AddAsync(new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id,
                        AssignedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
