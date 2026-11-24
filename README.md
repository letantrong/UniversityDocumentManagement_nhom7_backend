# University Centralized Document Management System (UCDMS) - Backend

Hệ thống Quản lý Tài liệu và Tiếp nhận Hồ sơ số hóa qua mã QR Code dành cho trường đại học.

## 🚀 Công nghệ sử dụng
- **Nền tảng:** .NET 10 (ASP.NET Core Web API)
- **Kiến trúc:** Clean Architecture (Domain - Application - Infrastructure - WebApi)
- **ORM & Database:** Entity Framework Core 10, Microsoft SQL Server
- **Xác thực & Phân quyền:** JWT Bearer (HMAC SHA-256), Refresh Token, Role-Based Access Control (RBAC)
- **Tài liệu API:** Scalar API Reference (`https://localhost:7028/scalar/v1`)
- **Mã hóa mật khẩu:** BCrypt.Net-Next

---

## 📂 Hướng dẫn dành cho các thành viên trong nhóm
Toàn bộ quy trình phát triển tính năng mới, phân công công việc chi tiết và quy chuẩn Git vui lòng xem tại:
👉 **[TÀI LIỆU HƯỚNG DẪN PHÁT TRIỂN MODULE BACKEND](docs/BACKEND_DEVELOPMENT_GUIDE.md)**

---

## 🛠️ Hướng dẫn chạy nhanh
1. Mở file `UniversityDocumentManagement.slnx` bằng Visual Studio 2026.
2. Kiểm tra chuỗi kết nối SQL Server trong `src/WebApi/appsettings.json`.
3. Bấm **F5** để khởi chạy ứng dụng. Database và dữ liệu mẫu (Seed Data) sẽ tự động được khởi tạo.
4. Truy cập tài liệu và test API tại: `https://localhost:7028/scalar/v1`.
   - **Tài khoản Quản trị viên:** `admin@ucdms.edu.vn`
   - **Mật khẩu:** `Admin@123456`
