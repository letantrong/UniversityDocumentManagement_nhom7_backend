using Application;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các tầng theo Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();


// 2.1 Cấu hình JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "YourSuperSecretKeyForDevelopmentPhaseOnlyChangeInProduction2026!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "UCDMS_Backend",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "UCDMS_Client",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Không cho phép độ trễ thời gian token
    };
});
builder.Services.AddAuthorization();


// 2. Cấu hình CORS cho Frontend (ReactJS)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 3. Cấu hình OpenAPI (tài liệu API)
builder.Services.AddOpenApi();

var app = builder.Build();

//// 4. Đăng ký Global Exception Handling Middleware (luôn đặt ở đầu pipeline HTTP)
//app.UseMiddleware<ExceptionHandlingMiddleware>();



// Đăng ký Middleware xử lý lỗi toàn cục tại đây (phải đặt ở đầu pipeline)
app.UseMiddleware<WebApi.Middlewares.ExceptionHandlingMiddleware>();


// 5. Giao diện OpenAPI / Scalar API Reference khi ở môi trường Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Truy cập tài liệu tại: /scalar/v1
}


app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Tự động chuyển hướng từ trang chủ vào trang tài liệu API
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

// Tự động kiểm tra Migration và Seed Data khi ứng dụng khởi chạy
using (var scope = app.Services.CreateScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ApplicationDbContextInitialiser>();
    await initialiser.InitialiseAsync();
    await initialiser.SeedAsync();
}


app.Run();
