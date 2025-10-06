using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Реализация валидатора запросов с поддержкой DataAnnotations и FluentValidation
/// </summary>
public class RequestValidator : IRequestValidator
{
    private readonly ILogger<RequestValidator> _logger;
    private readonly Dictionary<Type, Func<object, ValidationContext?, ValidationResult>> _validators;

    public RequestValidator(ILogger<RequestValidator> logger)
    {
        _logger = logger;
        _validators = new Dictionary<Type, Func<object, ValidationContext?, ValidationResult>>();
        RegisterDefaultValidators();
    }

    public async Task<ValidationResult> ValidateAsync<T>(T request)
    {
        return await ValidateAsync(request, null);
    }

    public async Task<ValidationResult> ValidateAsync<T>(T request, ValidationContext? validationContext = null)
    {
        if (request == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new() { PropertyName = "Request", Message = "Request object cannot be null" }
                }
            };
        }

        try
        {
            var type = typeof(T);
            _logger.LogDebug("Validating request of type {Type}", type.Name);

            // Используем кастомный валидатор если есть
            if (_validators.TryGetValue(type, out var customValidator))
            {
                var result = customValidator(request, validationContext);
                _logger.LogDebug("Custom validation completed for {Type}. Valid: {IsValid}", type.Name, result.IsValid);
                return result;
            }

            // Используем DataAnnotations валидацию
            var dataAnnotationsResult = await ValidateWithDataAnnotationsAsync(request, validationContext);
            _logger.LogDebug("DataAnnotations validation completed for {Type}. Valid: {IsValid}", type.Name, dataAnnotationsResult.IsValid);
            return dataAnnotationsResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation of {Type}", typeof(T).Name);
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new() { PropertyName = "Validation", Message = "Validation error occurred", ErrorCode = "VALIDATION_ERROR" }
                }
            };
        }
    }

    public bool CanValidate<T>()
    {
        var type = typeof(T);
        return _validators.ContainsKey(type) || HasDataAnnotations(type);
    }

    /// <summary>
    /// Зарегистрировать кастомный валидатор для типа
    /// </summary>
    public void RegisterValidator<T>(Func<T, ValidationContext?, ValidationResult> validator)
    {
        var type = typeof(T);
        _validators[type] = (obj, context) => validator((T)obj, context);
        _logger.LogDebug("Registered custom validator for type {Type}", type.Name);
    }

    private Task<ValidationResult> ValidateWithDataAnnotationsAsync<T>(T request, ValidationContext? validationContext)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = new List<ValidationError>();

        // Создаем контекст валидации если не предоставлен
        var context = validationContext ?? new ValidationContext(request!);

        // Валидируем объект
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(request!, context, validationResults, true);

        if (!isValid)
        {
            foreach (var validationResult in validationResults)
            {
                foreach (var memberName in validationResult.MemberNames)
                {
                    errors.Add(new ValidationError
                    {
                        PropertyName = memberName,
                        Message = validationResult.ErrorMessage ?? "Validation failed",
                        ErrorCode = "DATA_ANNOTATION_ERROR"
                    });
                }
            }
        }

        // Валидируем свойства
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var value = property.GetValue(request);
            var propertyContext = new ValidationContext(request!) { MemberName = property.Name };
            
            var propertyValidationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var propertyIsValid = Validator.TryValidateProperty(value, propertyContext, propertyValidationResults);

            if (!propertyIsValid)
            {
                foreach (var validationResult in propertyValidationResults)
                {
                    errors.Add(new ValidationError
                    {
                        PropertyName = property.Name,
                        Message = validationResult.ErrorMessage ?? "Property validation failed",
                        AttemptedValue = value,
                        ErrorCode = "PROPERTY_VALIDATION_ERROR"
                    });
                }
            }
        }

        result.IsValid = errors.Count == 0;
        result.Errors = errors;

        return Task.FromResult(result);
    }

    private bool HasDataAnnotations(Type type)
    {
        // Проверяем, есть ли атрибуты валидации на классе
        if (type.GetCustomAttributes<ValidationAttribute>().Any())
            return true;

        // Проверяем, есть ли атрибуты валидации на свойствах
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Any(p => p.GetCustomAttributes<ValidationAttribute>().Any());
    }

    private void RegisterDefaultValidators()
    {
        // Здесь можно зарегистрировать валидаторы по умолчанию для специфичных типов
        _logger.LogDebug("Registering default validators");

        RegisterValidator<CreateRequestDto>((request, context) => ValidateRequestWithItems(request, context));
        RegisterValidator<UpdateRequestDto>((request, context) => ValidateRequestWithItems(request, context));
    }

    private ValidationResult ValidateRequestWithItems(object request, ValidationContext? validationContext)
    {
        var baseResult = ValidateWithDataAnnotationsAsync(request, validationContext).GetAwaiter().GetResult();
        var errors = baseResult.Errors.ToList();

        ICollection<RequestItemInputDto>? items = request switch
        {
            CreateRequestDto createDto => createDto.Items,
            UpdateRequestDto updateDto => updateDto.Items,
            _ => null
        };

        if (items == null || items.Count == 0)
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(CreateRequestDto.Items),
                Message = "At least one item must be provided",
                ErrorCode = "REQUEST_NO_ITEMS"
            });
        }
        else
        {
            var index = 0;
            foreach (var item in items)
            {
                index++;
                if (item.ProductId <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        PropertyName = $"Items[{index - 1}].ProductId",
                        Message = "Product must be selected",
                        AttemptedValue = item.ProductId,
                        ErrorCode = "REQUEST_ITEM_PRODUCT_REQUIRED"
                    });
                }

                if (item.WarehouseId <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        PropertyName = $"Items[{index - 1}].WarehouseId",
                        Message = "Warehouse must be selected",
                        AttemptedValue = item.WarehouseId,
                        ErrorCode = "REQUEST_ITEM_WAREHOUSE_REQUIRED"
                    });
                }

                if (item.Quantity <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        PropertyName = $"Items[{index - 1}].Quantity",
                        Message = "Quantity must be greater than zero",
                        AttemptedValue = item.Quantity,
                        ErrorCode = "REQUEST_ITEM_QUANTITY_INVALID"
                    });
                }
            }
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
