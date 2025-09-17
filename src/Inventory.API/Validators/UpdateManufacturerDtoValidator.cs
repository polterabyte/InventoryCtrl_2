using FluentValidation;
using Inventory.Shared.DTOs;

namespace Inventory.API.Validators;

/// <summary>
/// Validator for UpdateManufacturerDto
/// </summary>
public class UpdateManufacturerDtoValidator : AbstractValidator<UpdateManufacturerDto>
{
    public UpdateManufacturerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Manufacturer name is required")
            .MinimumLength(2)
            .WithMessage("Manufacturer name must be at least 2 characters long")
            .MaximumLength(100)
            .WithMessage("Manufacturer name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.ContactInfo)
            .MaximumLength(200)
            .WithMessage("Contact info must not exceed 200 characters");

        RuleFor(x => x.Website)
            .Must(BeAValidUrl)
            .WithMessage("Website must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.Website));
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
