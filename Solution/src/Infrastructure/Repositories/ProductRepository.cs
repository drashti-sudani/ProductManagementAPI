using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Product> _dbSet;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(ApplicationDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context;
        _dbSet = _context.Set<Product>();
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        _logger.LogDebug("GetByIdAsync(Product) {Id}", id);
        return await _dbSet
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Product>> GetPagedAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        var skip = (page - 1) * pageSize;
        _logger.LogDebug("GetPagedAsync(Product) page={Page} pageSize={PageSize}", page, pageSize);
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Items)
            .OrderBy(p => p.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        _logger.LogDebug("AddAsync(Product) {ProductName}", product.ProductName);
        await _dbSet.AddAsync(product);
    }

    public void Update(Product product)
    {
        _logger.LogDebug("Update(Product) {ProductId}", product.Id);
        _dbSet.Update(product);
    }

    public void Delete(Product product)
    {
        _logger.LogDebug("Delete(Product) {ProductId}", product.Id);
        _dbSet.Remove(product);
    }
}
