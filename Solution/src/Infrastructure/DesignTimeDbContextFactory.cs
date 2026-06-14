using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Infrastructure.Data;

namespace Infrastructure;

/// <summary>
/// Design-time factory for <see cref="ApplicationDbContext"/>.
/// It attempts to read the connection string from a nearby appsettings.json (searching upward)
/// so migrations and Update-Database use the same DefaultConnection as the API project.
/// Falls back to a sensible default if no config is found.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Try to find an appsettings.json by searching upward from the current directory
        string? connectionString = null;
        var dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(dir, "appsettings.json");
            if (File.Exists(candidate))
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(dir)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .Build();

                connectionString = config.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrEmpty(connectionString)) break;
            }

            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }

        // If not found, try the common path relative to a typical solution layout
        if (string.IsNullOrEmpty(connectionString))
        {
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "API", "appsettings.json");
            if (File.Exists(fallback))
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(fallback)!)
                    .AddJsonFile(Path.GetFileName(fallback), optional: false, reloadOnChange: false)
                    .Build();

                connectionString = config.GetConnectionString("DefaultConnection");
            }
        }

        // Last-resort fallback (legacy value)
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Data Source=DRASHTI;Initial Catalog=DemoDataBase;Integrated Security=True;Encrypt=False";
        }

        builder.UseSqlServer(connectionString);
        return new ApplicationDbContext(builder.Options);
    }
}
