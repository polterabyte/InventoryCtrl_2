using FluentValidation;
using Inventory.Shared.DTOs;

namespace Inventory.API.Validators;

/// <summary>
/// Validator for CreateProductDto
/// </summary>
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MinimumLength(2)
            .WithMessage("Product name must be at least 2 characters long")
            .MaximumLength(200)
            .WithMessage("Product name must not exceed 200 characters");

        RuleFor(x => x.SKU)
            .NotEmpty()
            .WithMessage("SKU is required")
            .MinimumLength(3)
            .WithMessage("SKU must be at least 3 characters long")
            .MaximumLength(50)
            .WithMessage("SKU must not exceed 50 characters")
            .Matches("^[A-Z0-9-_]+$")
            .WithMessage("SKU can only contain uppercase letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Category ID must be greater than 0");

        RuleFor(x => x.UnitOfMeasureId)
            .GreaterThan(0)
            .WithMessage("Unit of measure ID must be greater than 0");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note must not exceed 500 characters");
    }
}
