using Inventory.Shared.DTOs;
using Inventory.Web.Client.Services.Validators;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Сервис для регистрации и настройки валидаторов
/// </summary>
public class ValidationService
{
    private readonly IRequestValidator _requestValidator;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(IRequestValidator requestValidator, ILogger<ValidationService> logger)
    {
        _requestValidator = requestValidator;
        _logger = logger;
    }

    /// <summary>
    /// Зарегистрировать все валидаторы
    /// </summary>
    public void RegisterAllValidators()
    {
        _logger.LogInformation("Registering all custom validators");

        // Регистрируем валидаторы для Manufacturer
        if (_requestValidator is RequestValidator validator)
        {
            validator.RegisterValidator<CreateManufacturerDto>(ManufacturerValidators.ValidateCreateManufacturer);
            validator.RegisterValidator<UpdateManufacturerDto>(ManufacturerValidators.ValidateUpdateManufacturer);

            // Регистрируем валидаторы для Category
            validator.RegisterValidator<CreateCategoryDto>(CategoryValidators.ValidateCreateCategory);
            validator.RegisterValidator<UpdateCategoryDto>(CategoryValidators.ValidateUpdateCategory);

            _logger.LogInformation("All custom validators registered successfully");
        }
        else
        {
            _logger.LogWarning("RequestValidator is not of expected type, custom validators not registered");
        }
    }
}
