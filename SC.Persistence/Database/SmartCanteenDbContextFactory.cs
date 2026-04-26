using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SC.Persistence.Database;

public class SmartCanteenDbContextFactory : IDesignTimeDbContextFactory<SmartCanteenDbContext>
{
    public SmartCanteenDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=smartcanteen;Username=smartcanteen;Password=smartcanteen;Include Error Detail=true";

        var optionsBuilder = new DbContextOptionsBuilder<SmartCanteenDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(SmartCanteenDbContext).Assembly.FullName);
        });

        return new SmartCanteenDbContext(optionsBuilder.Options);
    }
}