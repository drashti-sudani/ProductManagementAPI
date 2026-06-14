using FluentValidation;
using Application.DTOs;

namespace Application.Validators;

public class ProductCreateValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty()
            .MaximumLength(255);
    }
}
