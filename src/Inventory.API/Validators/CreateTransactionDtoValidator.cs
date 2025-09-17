using FluentValidation;
using Inventory.Shared.DTOs;
using Inventory.Shared.Constants;

namespace Inventory.API.Validators;

/// <summary>
/// Validator for CreateTransactionDto
/// </summary>
public class CreateTransactionDtoValidator : AbstractValidator<CreateInventoryTransactionDto>
{
    public CreateTransactionDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .WithMessage("Product ID must be greater than 0");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0)
            .WithMessage("Warehouse ID must be greater than 0");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Transaction type is required")
            .Must(type => type == "Income" || type == "Outcome" || type == "Install")
            .WithMessage("Transaction type must be Income, Outcome, or Install");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Transaction date cannot be in the future")
            .When(x => x.Date.HasValue);
    }
}
