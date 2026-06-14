using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Item> Items => Set<Item>();
    
    public DbSet<Domain.RefreshToken> RefreshTokens => Set<Domain.RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

}
