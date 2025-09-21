using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно быть от 3 до 50 символов")]
    public required string Username { get; set; }
    
    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public required string Password { get; set; }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно быть от 3 до 50 символов")]
    public required string Username { get; set; }
    
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public required string Email { get; set; }
    
    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public required string Password { get; set; }
    
    [Required(ErrorMessage = "Подтверждение пароля обязательно")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public required string ConfirmPassword { get; set; }
}

public class RefreshRequest
{
    public required string Username { get; set; }
    public required string RefreshToken { get; set; }
}

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Базовый класс для всех API ответов
/// </summary>
public abstract class ApiResponseBase
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public int? StatusCode { get; set; }

}

/// <summary>
/// API ответ с данными
/// </summary>
public class ApiResponse<T> : ApiResponseBase
{
    public T? Data { get; set; }

    /// <summary>
    /// Создает успешный ответ с данными
    /// </summary>
    public static ApiResponse<T> CreateSuccess(T data, string? requestId = null, int? statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            RequestId = requestId,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Создает ответ с ошибкой
    /// </summary>
    public static ApiResponse<T> CreateFailure(string errorMessage, List<string>? errors = null, string? requestId = null, int? statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            Errors = errors ?? new(),
            RequestId = requestId,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Создает ответ с ошибкой валидации
    /// </summary>
    public static ApiResponse<T> CreateValidationFailure(List<string> validationErrors, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = "Validation failed",
            Errors = validationErrors,
            RequestId = requestId,
            StatusCode = 400
        };
    }
}

/// <summary>
/// Пагинированный ответ
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// API ответ с пагинированными данными
/// </summary>
public class PagedApiResponse<T> : ApiResponseBase
{
    public PagedResponse<T>? Data { get; set; }

    /// <summary>
    /// Создает успешный пагинированный ответ
    /// </summary>
    public static PagedApiResponse<T> CreateSuccess(PagedResponse<T> data, string? requestId = null, int? statusCode = 200)
    {
        return new PagedApiResponse<T>
        {
            Success = true,
            Data = data,
            RequestId = requestId,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Создает пагинированный ответ с ошибкой
    /// </summary>
    public static PagedApiResponse<T> CreateFailure(string errorMessage, List<string>? errors = null, string? requestId = null, int? statusCode = 400)
    {
        return new PagedApiResponse<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            Errors = errors ?? new(),
            RequestId = requestId,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Создает пагинированный ответ с ошибкой валидации
    /// </summary>
    public static PagedApiResponse<T> CreateValidationFailure(List<string> validationErrors, string? requestId = null)
    {
        return new PagedApiResponse<T>
        {
            Success = false,
            ErrorMessage = "Validation failed",
            Errors = validationErrors,
            RequestId = requestId,
            StatusCode = 400
        };
    }
}

// DTOs with primary constructors for common operations
public record CreateEntityResult<T>(bool Success, T? Entity, string? ErrorMessage);

public record UpdateEntityResult<T>(bool Success, T? Entity, string? ErrorMessage);

public record DeleteEntityResult(bool Success, string? ErrorMessage);

public record ValidationResult(bool IsValid, List<string> Errors);

// Configuration DTOs with primary constructors
public record DatabaseConfiguration(string ConnectionString, int CommandTimeout, bool EnableRetryOnFailure);

public record LoggingConfiguration(string LogLevel, string LogPath, bool EnableConsoleLogging);

public record ApiConfiguration(string BaseUrl, int TimeoutSeconds, bool EnableCompression);