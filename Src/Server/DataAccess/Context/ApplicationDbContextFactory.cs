using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Context;

/*  IMPORTANT !!
    DON'T DELETE THIS CLASS 
    
    -------------

    This Factory class is used to created a db context at design time on the scaffolding of a controller
    Since DbContext is in a different project from the startup one, this class is needed
    Reference to:
    https://stackoverflow.com/questions/71196547/visual-studio-2022-net-6-scaffolding-doesnt-work-when-dbcontext-is-in-a-separa
    https://github.com/dotnet/Scaffolding/issues/1765#issuecomment-1058674843
 
 */

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DataContext(optionsBuilder.Options);
    }
}