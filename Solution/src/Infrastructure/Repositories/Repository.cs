using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;
    private readonly ILogger<Repository<T>> _logger;

    public Repository(ApplicationDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = _context.Set<T>();
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        _logger.LogDebug("GetByIdAsync<{Entity}>({Id})", typeof(T).Name, id);
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        _logger.LogDebug("GetAllAsync<{Entity}> (AsNoTracking)", typeof(T).Name);
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        _logger.LogDebug("AddAsync<{Entity}>", typeof(T).Name);
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _logger.LogDebug("Update<{Entity}>", typeof(T).Name);
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _logger.LogDebug("Delete<{Entity}>", typeof(T).Name);
        _dbSet.Remove(entity);
    }
}
