using ApiApp.Configuration;
using ApiApp.Helpers;
using ApiApp.Middlewares;
using ApiApp.Services;
using DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Startup;

public static class ServicesConfiguration
{
    public static void AddServices(this IServiceCollection services, ConfigurationManager _configuration)
    {
        // Configurations
        var jwtConfigurationSection = _configuration.GetSection("JwtConfiguration");
        JwtConfiguration jwtConfiguration = jwtConfigurationSection.Get<JwtConfiguration>()!; // If need to use in the startup
        services.Configure<JwtConfiguration>(_configuration.GetSection("JwtConfiguration"));

        // Automapper
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Database
        services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("DataAccess")));

        // Middlewares
        services.AddTransient<ErrorMiddleware>();

        // Helpers
        services.AddTransient<IAuthenticationHelper, AuthenticationHelper>();

        // Services
        services.AddTransient<IUserService, UserService>();
    }
}
