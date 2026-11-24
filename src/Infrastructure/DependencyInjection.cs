using Application.Common.Interfaces;
using Infrastructure.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Features.Auth; 


namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b =>
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Đăng ký IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Đăng ký dịch vụ PasswordHasher và DbInitialiser
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ApplicationDbContextInitialiser>();

        // Đăng ký dịch vụ phát hành JWT Token
        services.AddScoped<IJwtTokenGenerator, Infrastructure.Identity.JwtTokenGenerator>();

        // Trong hàm AddApplication:
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
