using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ProductCreateDto> _validator;
    private readonly Microsoft.Extensions.Logging.ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IValidator<ProductCreateDto> validator,
        Microsoft.Extensions.Logging.ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
    {
        _logger?.LogInformation("Creating product {ProductName}", dto.ProductName);
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger?.LogWarning("ProductCreateDto validation failed: {Errors}", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException(validationResult.Errors);
        }

        var entity = new Product
        {
            ProductName = dto.ProductName,
            CreatedOn = System.DateTime.UtcNow,
            CreatedBy = string.Empty
        };

        await _productRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        _logger?.LogInformation("Created product {ProductId} ({ProductName})", entity.Id, entity.ProductName);
        return new ProductResponseDto
        {
            Id = entity.Id,
            ProductName = entity.ProductName
        };
    }

    public async Task<ProductResponseDto?> GetByIdAsync(int id)
    {
        _logger?.LogInformation("Getting product by id {Id}", id);
        var entity = await _productRepository.GetByIdAsync(id);
        if (entity is null) return null;

        return new ProductResponseDto
        {
            Id = entity.Id,
            ProductName = entity.ProductName
        };
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllAsync(int page, int pageSize)
    {
        _logger?.LogInformation("Getting products page {Page} size {PageSize}", page, pageSize);
        var entities = await _productRepository.GetPagedAsync(page, pageSize);
        return entities.Select(e => new ProductResponseDto
        {
            Id = e.Id,
            ProductName = e.ProductName
        });
    }
}
