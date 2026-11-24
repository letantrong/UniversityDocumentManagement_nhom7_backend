using Application.Features.Auth;
using Application.Features.Departments;
using Application.Features.Roles;
using Application.Features.Stations;
using Application.Features.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddValidatorsFromAssembly(assembly);

        // Đăng ký dịch vụ xác thực

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IDepartmentService, DepartmentService>();

        services.AddScoped<IStationService, StationService>();

        services.AddScoped<IRoleService, RoleService>();

        services.AddScoped<IUserService, UserService>();


        return services;
    }
}
