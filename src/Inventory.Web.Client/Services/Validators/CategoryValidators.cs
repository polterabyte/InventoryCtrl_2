using Inventory.Shared.DTOs;
using Inventory.Web.Client.Services;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Web.Client.Services.Validators;

/// <summary>
/// Валидаторы для Category DTO
/// </summary>
public static class CategoryValidators
{
    /// <summary>
    /// Валидатор для CreateCategoryDto
    /// </summary>
    public static ValidationResult ValidateCreateCategory(CreateCategoryDto dto, ValidationContext? context)
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
                    Message = "Category name cannot be empty or contain only whitespace",
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
                    Message = "Category name cannot contain consecutive spaces",
                    AttemptedValue = dto.Name,
                    ErrorCode = "INVALID_NAME_FORMAT"
                });
            }

            // Проверяем на специальные символы, которые могут вызвать проблемы
            var invalidChars = new[] { '<', '>', '&', '"', '\'', '/', '\\' };
            if (dto.Name.IndexOfAny(invalidChars) >= 0)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(dto.Name),
                    Message = "Category name cannot contain special characters: < > & \" ' / \\",
                    AttemptedValue = dto.Name,
                    ErrorCode = "INVALID_NAME_CHARACTERS"
                });
            }
        }

        // Валидация родительской категории
        if (dto.ParentCategoryId.HasValue)
        {
            if (dto.ParentCategoryId.Value <= 0)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(dto.ParentCategoryId),
                    Message = "Parent category ID must be a positive number",
                    AttemptedValue = dto.ParentCategoryId.Value,
                    ErrorCode = "INVALID_PARENT_CATEGORY_ID"
                });
            }
        }

        result.IsValid = errors.Count == 0;
        result.Errors = errors;
        return result;
    }

    /// <summary>
    /// Валидатор для UpdateCategoryDto
    /// </summary>
    public static ValidationResult ValidateUpdateCategory(UpdateCategoryDto dto, ValidationContext? context)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = new List<ValidationError>();

        // Применяем те же правила, что и для создания
        var createDto = new CreateCategoryDto
        {
            Name = dto.Name,
            Description = dto.Description,
            ParentCategoryId = dto.ParentCategoryId
        };

        var createValidation = ValidateCreateCategory(createDto, context);
        if (!createValidation.IsValid)
        {
            errors.AddRange(createValidation.Errors);
        }

        result.IsValid = errors.Count == 0;
        result.Errors = errors;
        return result;
    }
}
