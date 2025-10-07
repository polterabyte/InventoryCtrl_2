using FluentValidation;
using Inventory.Shared.DTOs;

namespace Inventory.API.Validators;

public class CreateKanbanCardDtoValidator : AbstractValidator<CreateKanbanCardDto>
{
    public CreateKanbanCardDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId must be greater than 0");
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("WarehouseId must be greater than 0");
        RuleFor(x => x.MinThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("MinThreshold must be >= 0");
        RuleFor(x => x.MaxThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("MaxThreshold must be >= 0");
        RuleFor(x => x)
            .Must(x => x.MaxThreshold >= x.MinThreshold)
            .WithMessage("MaxThreshold must be >= MinThreshold");
    }
}
