using Inventory.Shared.DTOs;
using Inventory.Web.Client.Services;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Web.Client.Services.Validators;

public static class ManufacturerValidators
{
    public static ValidationResult ValidateCreateManufacturer(CreateManufacturerDto dto, ValidationContext? context)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = new List<ValidationError>();

        if (!string.IsNullOrEmpty(dto.Name))
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(dto.Name),
                    Message = "Manufacturer name cannot be empty or contain only whitespace",
                    AttemptedValue = dto.Name,
                    ErrorCode = "INVALID_NAME_FORMAT"
                });
            }

            if (dto.Name.Contains("  "))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(dto.Name),
                    Message = "Manufacturer name cannot contain consecutive spaces",
                    AttemptedValue = dto.Name,
                    ErrorCode = "INVALID_NAME_FORMAT"
                });
            }
        }

        if (!string.IsNullOrEmpty(dto.Website))
        {
            if (!Uri.TryCreate(dto.Website, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(dto.Website),
                    Message = "Website must be a valid HTTP or HTTPS URL",
                    AttemptedValue = dto.Website,
                    ErrorCode = "INVALID_URL_FORMAT"
                });
            }
        }

        if (!string.IsNullOrEmpty(dto.ContactInfo))
        {
            if (dto.ContactInfo.Contains("@") && !IsValidEmailFormat(dto.ContactInfo))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(dto.ContactInfo),
                    Message = "Contact info appears to be an email but has invalid format",
                    AttemptedValue = dto.ContactInfo,
                    ErrorCode = "INVALID_EMAIL_FORMAT"
                });
            }
        }

        result.IsValid = errors.Count == 0;
        result.Errors = errors;
        return result;
    }

    public static ValidationResult ValidateUpdateManufacturer(UpdateManufacturerDto dto, ValidationContext? context)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = new List<ValidationError>();

        var createDto = new CreateManufacturerDto
        {
            Name = dto.Name,
            Description = dto.Description,
            ContactInfo = dto.ContactInfo,
            Website = dto.Website
        };

        var createValidation = ValidateCreateManufacturer(createDto, context);
        if (!createValidation.IsValid)
        {
            errors.AddRange(createValidation.Errors);
        }

        result.IsValid = errors.Count == 0;
        result.Errors = errors;
        return result;
    }

    private static bool IsValidEmailFormat(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

