using FluentValidation;
using Inventory.Shared.DTOs;
using Inventory.Shared.Constants;

namespace Inventory.API.Validators;

/// <summary>
/// Validator for UpdateTransactionDto
/// </summary>
public class UpdateTransactionDtoValidator : AbstractValidator<UpdateInventoryTransactionDto>
{
    public UpdateTransactionDtoValidator()
    {
        RuleFor(x => x.LocationId)
            .GreaterThan(0)
            .WithMessage("Location ID must be greater than 0")
            .When(x => x.LocationId.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");
    }
}
