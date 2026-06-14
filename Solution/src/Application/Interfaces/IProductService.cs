using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces;

public interface IProductService
{
    Task<ProductResponseDto> CreateAsync(ProductCreateDto dto);

    Task<ProductResponseDto?> GetByIdAsync(int id);

    Task<IEnumerable<ProductResponseDto>> GetAllAsync(int page, int pageSize);
}
