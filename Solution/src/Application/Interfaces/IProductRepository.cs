using System.Collections.Generic;
using System.Threading.Tasks;
using Domain;

namespace Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);

    Task<IEnumerable<Product>> GetPagedAsync(
        int page,
        int pageSize);

    Task AddAsync(Product product);

    void Update(Product product);

    void Delete(Product product);
}
