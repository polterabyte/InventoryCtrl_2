using System.ComponentModel.DataAnnotations;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Интерфейс для валидации API запросов
/// </summary>
public interface IRequestValidator
{
    /// <summary>
    /// Валидировать объект запроса
    /// </summary>
    /// <typeparam name="T">Тип объекта для валидации</typeparam>
    /// <param name="request">Объект запроса</param>
    /// <returns>Результат валидации</returns>
    Task<ValidationResult> ValidateAsync<T>(T request);
    
    /// <summary>
    /// Валидировать объект запроса с кастомным контекстом
    /// </summary>
    /// <typeparam name="T">Тип объекта для валидации</typeparam>
    /// <param name="request">Объект запроса</param>
    /// <param name="validationContext">Контекст валидации</param>
    /// <returns>Результат валидации</returns>
    Task<ValidationResult> ValidateAsync<T>(T request, ValidationContext? validationContext = null);
    
    /// <summary>
    /// Проверить, поддерживается ли валидация для указанного типа
    /// </summary>
    /// <typeparam name="T">Тип для проверки</typeparam>
    /// <returns>True, если валидация поддерживается</returns>
    bool CanValidate<T>();
}

/// <summary>
/// Результат валидации
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public string? Summary => Errors.Count > 0 ? string.Join("; ", Errors.Select(e => e.Message)) : null;
}

/// <summary>
/// Ошибка валидации
/// </summary>
public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
    public string? ErrorCode { get; set; }
}
