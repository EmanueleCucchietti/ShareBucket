using ApiApp.Middlewares;
using DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Startup;

public static class ServicesConfiguration
{
    public static void AddServices(this IServiceCollection services, ConfigurationManager _configuration)
    {
        services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("DataAccess")));

        services.AddTransient<ErrorMiddleware>();


    }
}
