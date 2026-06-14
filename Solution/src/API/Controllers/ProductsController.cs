using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.Interfaces;
using Application.DTOs;
using Domain;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService service, IProductRepository repository, IUnitOfWork unitOfWork, ILogger<ProductsController> logger)
    {
        _service = service;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("GetAll called with page={Page} pageSize={PageSize}", page, pageSize);
        var items = await _service.GetAllAsync(page, pageSize);
        _logger.LogInformation("GetAll returned {Count} items", items?.Count() ?? 0);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        _logger.LogInformation("Get called for id={Id}", id);
        var product = await _service.GetByIdAsync(id);
        if (product is null)
        {
            _logger.LogWarning("Product not found id={Id}", id);
            return NotFound();
        }
        _logger.LogInformation("Product found id={Id}", id);
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        _logger.LogInformation("Create called with ProductName={ProductName}", dto.ProductName);
        var created = await _service.CreateAsync(dto);
        _logger.LogInformation("Product created id={Id} name={ProductName}", created.Id, created.ProductName);
        return CreatedAtAction(nameof(Get), new { id = created.Id, version = "1.0" }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductCreateDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return NotFound();
        _logger.LogInformation("Update called for id={Id} with ProductName={ProductName}", id, dto.ProductName);
        entity.ProductName = dto.ProductName;
        entity.ModifiedOn = System.DateTime.UtcNow;
        entity.ModifiedBy = string.Empty;

        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Product updated id={Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return NotFound();
        _logger.LogInformation("Delete called for id={Id}", id);
        _repository.Delete(entity);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Product deleted id={Id}", id);
        return NoContent();
    }

    [HttpGet("{id}/items")]
    public async Task<IActionResult> GetItems(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return NotFound();
        _logger.LogInformation("GetItems called for product id={Id}", id);
        var items = entity.Items.Select(i => new ItemResponseDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity
        });

        _logger.LogInformation("GetItems returning {Count} items for product id={Id}", items.Count(), id);
        return Ok(items);
    }
}
