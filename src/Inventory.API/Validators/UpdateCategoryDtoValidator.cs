using FluentValidation;
using Inventory.Shared.DTOs;

namespace Inventory.API.Validators;

/// <summary>
/// Validator for UpdateCategoryDto
/// </summary>
public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required")
            .MinimumLength(2)
            .WithMessage("Category name must be at least 2 characters long")
            .MaximumLength(100)
            .WithMessage("Category name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.ParentCategoryId)
            .GreaterThan(0)
            .WithMessage("Parent category ID must be greater than 0")
            .When(x => x.ParentCategoryId.HasValue);
    }
}
