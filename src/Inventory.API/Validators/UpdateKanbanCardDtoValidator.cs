using FluentValidation;
using Inventory.Shared.DTOs;

namespace Inventory.API.Validators;

public class UpdateKanbanCardDtoValidator : AbstractValidator<UpdateKanbanCardDto>
{
    public UpdateKanbanCardDtoValidator()
    {
        RuleFor(x => x.MinThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("MinThreshold must be >= 0");
        RuleFor(x => x.MaxThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("MaxThreshold must be >= 0");
        RuleFor(x => x)
            .Must(x => x.MaxThreshold >= x.MinThreshold)
            .WithMessage("MaxThreshold must be >= MinThreshold");
    }
}
