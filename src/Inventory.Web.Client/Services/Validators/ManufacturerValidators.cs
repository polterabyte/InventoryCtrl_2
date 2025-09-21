using Inventory.Shared.DTOs;
using Inventory.Web.Client.Services;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Web.Client.Services.Validators;

/// <summary>
/// Валидаторы для Manufacturer DTO
/// </summary>
public static class ManufacturerValidators
{
    /// <summary>
    /// Валидатор для CreateManufacturerDto
    /// </summary>
    public static ValidationResult ValidateCreateManufacturer(CreateManufacturerDto dto, ValidationContext? context)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = new List<ValidationError>();

        // Дополнительная бизнес-логика валидации
        if (!string.IsNullOrEmpty(dto.Name))
        {
            // Проверяем, что название не содержит только пробелы
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

            // Проверяем на запрещенные символы
            if (dto.Name.Contains("  ")) // Двойные пробелы
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

        // Валидация веб-сайта
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

        // Валидация контактной информации
        if (!string.IsNullOrEmpty(dto.ContactInfo))
        {
            // Проверяем, что контактная информация содержит хотя бы один символ @ для email
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

    /// <summary>
    /// Валидатор для UpdateManufacturerDto
    /// </summary>
    public static ValidationResult ValidateUpdateManufacturer(UpdateManufacturerDto dto, ValidationContext? context)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = new List<ValidationError>();

        // Применяем те же правила, что и для создания
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
