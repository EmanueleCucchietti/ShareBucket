using ApiApp.Configuration;
using ApiApp.Helpers;
using ApiApp.Middlewares;
using ApiApp.Services;
using DataAccess.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
        services.AddTransient<IAesEncryptionService, AesEncryptionService>();

        // Services
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IFileService, FileService>();
        

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

        // Cors
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:8080",
                    "https://localhost:8443")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Configuration for files transferring and encrypting
        services.Configure<KestrelServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
        });
        services.Configure<IISServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
        });


    }
}
