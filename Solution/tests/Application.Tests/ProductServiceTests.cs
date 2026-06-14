using System.Threading.Tasks;
using Application.Services;
using Application.DTOs;
using Application.Interfaces;
using Domain;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using FluentAssertions;

namespace Application.Tests;

public class ProductServiceTests
{
    private readonly ITestOutputHelper _output;

    public ProductServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public async Task CreateAsync_ValidDto_CreatesProduct()
    {
        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Returns(() => Task.CompletedTask);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var validatorMock = new Mock<IValidator<ProductCreateDto>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ProductCreateDto>(), default))
            .ReturnsAsync(new ValidationResult());

        var service = new ProductService(repoMock.Object, uowMock.Object, validatorMock.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ProductService>>());

        var dto = new ProductCreateDto { ProductName = "Test" };
        _output.WriteLine("Input: {0}", JsonSerializer.Serialize(dto));
        var result = await service.CreateAsync(dto);
        _output.WriteLine("Result: {0}", JsonSerializer.Serialize(result));

        result.Should().NotBeNull();
        result.ProductName.Should().Be("Test");

        repoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidDto_ThrowsValidationException()
    {
        var repoMock = new Mock<IProductRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        var failures = new[] { new ValidationFailure("ProductName", "Required") };
        var validatorMock = new Mock<IValidator<ProductCreateDto>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ProductCreateDto>(), default))
            .ReturnsAsync(new ValidationResult(failures));

        var service = new ProductService(repoMock.Object, uowMock.Object, validatorMock.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ProductService>>());

        var dto = new ProductCreateDto { ProductName = string.Empty };
        _output.WriteLine("Input (invalid): {0}", JsonSerializer.Serialize(dto));
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
    }
}
