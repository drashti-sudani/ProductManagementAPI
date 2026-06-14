using System.Threading.Tasks;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task Repository_GetAllAsync_ReturnsInsertedEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "RepoTestDb")
            .Options;

        using var context = new ApplicationDbContext(options);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<Repository<Domain.Product>>.Instance;
        var repo = new Repository<Domain.Product>(context, logger);

        var entity = new Domain.Product { ProductName = "t1", CreatedBy = "test", CreatedOn = System.DateTime.UtcNow };
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        all.Should().ContainSingle();
    }

}
