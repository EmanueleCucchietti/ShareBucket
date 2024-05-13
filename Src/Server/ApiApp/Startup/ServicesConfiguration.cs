using ApiApp.Configuration;
using ApiApp.Helpers;
using ApiApp.Middlewares;
using ApiApp.Services;
using DataAccess.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
        services.AddTransient<AuthenticationMiddleware>();

        // Helpers
        services.AddTransient<IAuthenticationHelper, AuthenticationHelper>();

        // Services
        services.AddTransient<IUserService, UserService>();

        // Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfiguration.Issuer,
                    ValidAudience = jwtConfiguration.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        jwtConfiguration.Key))
                };
            });
    }
}
