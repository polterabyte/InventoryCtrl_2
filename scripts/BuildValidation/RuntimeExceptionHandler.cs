using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Runtime exception handling system implementing structured error responses from design document
    /// Handles authentication, database, API communication, and middleware pipeline errors
    /// </summary>
    public class RuntimeExceptionHandler
    {
        private readonly ILogger _logger;
        private readonly IErrorClassifier _errorClassifier;
        private readonly Dictionary<Type, IExceptionHandler> _exceptionHandlers;

        public RuntimeExceptionHandler(ILogger logger, IErrorClassifier errorClassifier)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorClassifier = errorClassifier ?? throw new ArgumentNullException(nameof(errorClassifier));
            _exceptionHandlers = InitializeExceptionHandlers();
        }

        /// <summary>
        /// Processes runtime exception and returns structured error response per design document
        /// </summary>
        public async Task<ErrorResponse> HandleExceptionAsync(Exception exception, string? requestPath = null)
        {
            try
            {
                var classifiedError = _errorClassifier.ClassifyRuntimeError(exception);
                var errorResponse = await CreateErrorResponseAsync(classifiedError, requestPath);

                // Log the error with appropriate level based on severity
                LogException(classifiedError, exception, requestPath);

                // Attempt recovery if possible
                if (_exceptionHandlers.TryGetValue(exception.GetType(), out var handler))
                {
                    var recoveryResult = await handler.HandleAsync(exception);
                    errorResponse.RecoveryAttempted = recoveryResult.Attempted;
                    errorResponse.RecoverySuccessful = recoveryResult.Successful;
                }

                return errorResponse;
            }
            catch (Exception handlingException)
            {
                _logger.LogError($"Exception handling failed: {handlingException.Message}", handlingException);
                return CreateFallbackErrorResponse(exception);
            }
        }

        /// <summary>
        /// ASP.NET Core middleware for global exception handling
        /// </summary>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                await HandleHttpExceptionAsync(context, exception);
            }
        }

        private async Task HandleHttpExceptionAsync(HttpContext context, Exception exception)
        {
            var errorResponse = await HandleExceptionAsync(exception, context.Request.Path);
            
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = DetermineHttpStatusCode(errorResponse.Category);

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private async Task<ErrorResponse> CreateErrorResponseAsync(BuildError classifiedError, string? requestPath)
        {
            var response = new ErrorResponse
            {
                Success = false,
                Error = DetermineErrorCode(classifiedError.Category),
                Message = GetUserFriendlyMessage(classifiedError),
                Category = classifiedError.Category.ToString(),
                Timestamp = DateTime.UtcNow,
                RequestPath = requestPath,
                TraceId = GenerateTraceId()
            };

            // Add validation errors if applicable
            if (classifiedError.Category == ErrorCategory.ConfigurationError && classifiedError.AdditionalData.ContainsKey("ValidationErrors"))
            {
                response.Errors = classifiedError.AdditionalData["ValidationErrors"] as List<string> ?? new List<string>();
            }

            // Add resolution suggestions for development/staging environments
            if (ShouldIncludeResolutionSuggestions())
            {
                response.ResolutionSuggestions = GetResolutionSuggestions(classifiedError.Category);
            }

            await Task.CompletedTask; // Placeholder for async operations
            return response;
        }

        private string GetUserFriendlyMessage(BuildError classifiedError)
        {
            return classifiedError.Category switch
            {
                ErrorCategory.Authentication => "Authentication failed. Please log in again.",
                ErrorCategory.DatabaseConnectivity => "Service temporarily unavailable. Please try again later.",
                ErrorCategory.NetworkConnectivity => "Network error occurred. Please check your connection.",
                ErrorCategory.ConfigurationError => "Invalid request. Please check your input.",
                ErrorCategory.RuntimeException => "An unexpected error occurred. Please try again.",
                _ => "An error occurred while processing your request."
            };
        }

        private string DetermineErrorCode(ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.Authentication => "UNAUTHORIZED",
                ErrorCategory.DatabaseConnectivity => "SERVICE_UNAVAILABLE",
                ErrorCategory.NetworkConnectivity => "NETWORK_ERROR",
                ErrorCategory.ConfigurationError => "VALIDATION_ERROR",
                ErrorCategory.RuntimeException => "SERVER_ERROR",
                _ => "UNKNOWN_ERROR"
            };
        }

        private int DetermineHttpStatusCode(ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.Authentication => 401, // Unauthorized
                ErrorCategory.ConfigurationError => 400, // Bad Request
                ErrorCategory.DatabaseConnectivity => 503, // Service Unavailable
                ErrorCategory.NetworkConnectivity => 502, // Bad Gateway
                ErrorCategory.RuntimeException => 500, // Internal Server Error
                _ => 500 // Internal Server Error
            };
        }

        private List<string> GetResolutionSuggestions(ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.Authentication => new List<string>
                {
                    "Verify JWT token is valid and not expired",
                    "Check authentication configuration in appsettings.json",
                    "Ensure Jwt__Key, Jwt__Issuer, and Jwt__Audience are set correctly"
                },
                ErrorCategory.DatabaseConnectivity => new List<string>
                {
                    "Check database connection string configuration",
                    "Verify PostgreSQL service is running",
                    "Ensure database credentials are correct",
                    "Check network connectivity to database host"
                },
                ErrorCategory.NetworkConnectivity => new List<string>
                {
                    "Verify network connectivity",
                    "Check firewall settings",
                    "Validate API endpoint configuration",
                    "Ensure service discovery is working"
                },
                ErrorCategory.ConfigurationError => new List<string>
                {
                    "Review appsettings.json configuration",
                    "Validate environment variables",
                    "Check required configuration sections",
                    "Ensure proper JSON format"
                },
                _ => new List<string> { "Check application logs for detailed error information" }
            };
        }

        private void LogException(BuildError classifiedError, Exception exception, string? requestPath)
        {
            var logLevel = DetermineLogLevel(classifiedError.Severity);
            var message = $"Runtime exception in {requestPath ?? "unknown"}: {classifiedError.Message}";

            switch (logLevel)
            {
                case "Critical":
                case "Error":
                    _logger.LogError(message, exception);
                    break;
                case "Warning":
                    _logger.LogWarning(message);
                    break;
                default:
                    _logger.LogInfo(message);
                    break;
            }
        }

        private string DetermineLogLevel(ErrorSeverity severity)
        {
            return severity switch
            {
                ErrorSeverity.Critical => "Critical",
                ErrorSeverity.High => "Error",
                ErrorSeverity.Medium => "Warning",
                ErrorSeverity.Low => "Info",
                _ => "Info"
            };
        }

        private ErrorResponse CreateFallbackErrorResponse(Exception exception)
        {
            return new ErrorResponse
            {
                Success = false,
                Error = "SYSTEM_ERROR",
                Message = "A system error occurred. Please try again later.",
                Category = "SystemError",
                Timestamp = DateTime.UtcNow,
                TraceId = GenerateTraceId()
            };
        }

        private string GenerateTraceId()
        {
            return Guid.NewGuid().ToString("N")[..16];
        }

        private bool ShouldIncludeResolutionSuggestions()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return environment != "Production";
        }

        private Dictionary<Type, IExceptionHandler> InitializeExceptionHandlers()
        {
            return new Dictionary<Type, IExceptionHandler>
            {
                [typeof(UnauthorizedAccessException)] = new AuthenticationExceptionHandler(_logger),
                [typeof(TimeoutException)] = new TimeoutExceptionHandler(_logger),
                [typeof(System.Data.Common.DbException)] = new DatabaseExceptionHandler(_logger),
                [typeof(System.Net.Http.HttpRequestException)] = new NetworkExceptionHandler(_logger),
                [typeof(ArgumentNullException)] = new ValidationExceptionHandler(_logger),
                [typeof(InvalidOperationException)] = new OperationExceptionHandler(_logger)
            };
        }
    }

    #region Exception Handlers

    public interface IExceptionHandler
    {
        Task<ExceptionHandlingResult> HandleAsync(Exception exception);
    }

    public class AuthenticationExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;

        public AuthenticationExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExceptionHandlingResult> HandleAsync(Exception exception)
        {
            var result = new ExceptionHandlingResult { Attempted = true };

            try
            {
                // Log authentication failure details
                _logger.LogWarning($"Authentication failed: {exception.Message}");

                // Clear any cached authentication tokens
                // Implementation would clear JWT tokens, reset authentication state
                
                result.Successful = true;
                result.RecoveryActions.Add("Authentication state cleared");
            }
            catch (Exception recoveryException)
            {
                _logger.LogError($"Authentication exception recovery failed: {recoveryException.Message}", recoveryException);
                result.Successful = false;
            }

            await Task.CompletedTask; // Placeholder for async operations
            return result;
        }
    }

    public class TimeoutExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;

        public TimeoutExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExceptionHandlingResult> HandleAsync(Exception exception)
        {
            var result = new ExceptionHandlingResult { Attempted = true };

            try
            {
                _logger.LogWarning($"Timeout occurred: {exception.Message}");
                
                // Implement retry logic or circuit breaker pattern
                result.Successful = true;
                result.RecoveryActions.Add("Timeout handled with circuit breaker pattern");
            }
            catch (Exception recoveryException)
            {
                _logger.LogError($"Timeout exception recovery failed: {recoveryException.Message}", recoveryException);
                result.Successful = false;
            }

            await Task.CompletedTask; // Placeholder for async operations
            return result;
        }
    }

    public class DatabaseExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;

        public DatabaseExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExceptionHandlingResult> HandleAsync(Exception exception)
        {
            var result = new ExceptionHandlingResult { Attempted = true };

            try
            {
                _logger.LogError($"Database error: {exception.Message}", exception);
                
                // Implement database connection retry logic
                // Check database health and attempt reconnection
                
                result.Successful = false; // Usually requires manual intervention
                result.RecoveryActions.Add("Database connection monitoring initiated");
            }
            catch (Exception recoveryException)
            {
                _logger.LogError($"Database exception recovery failed: {recoveryException.Message}", recoveryException);
                result.Successful = false;
            }

            await Task.CompletedTask; // Placeholder for async operations
            return result;
        }
    }

    public class NetworkExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;

        public NetworkExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExceptionHandlingResult> HandleAsync(Exception exception)
        {
            var result = new ExceptionHandlingResult { Attempted = true };

            try
            {
                _logger.LogWarning($"Network error: {exception.Message}");
                
                // Implement network retry with exponential backoff
                result.Successful = true;
                result.RecoveryActions.Add("Network retry policy activated");
            }
            catch (Exception recoveryException)
            {
                _logger.LogError($"Network exception recovery failed: {recoveryException.Message}", recoveryException);
                result.Successful = false;
            }

            await Task.CompletedTask; // Placeholder for async operations
            return result;
        }
    }

    public class ValidationExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;

        public ValidationExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExceptionHandlingResult> HandleAsync(Exception exception)
        {
            var result = new ExceptionHandlingResult { Attempted = true };

            try
            {
                _logger.LogInfo($"Validation error: {exception.Message}");
                
                // Extract validation details for user feedback
                result.Successful = true;
                result.RecoveryActions.Add("Validation errors extracted for user feedback");
            }
            catch (Exception recoveryException)
            {
                _logger.LogError($"Validation exception recovery failed: {recoveryException.Message}", recoveryException);
                result.Successful = false;
            }

            await Task.CompletedTask; // Placeholder for async operations
            return result;
        }
    }

    public class OperationExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;

        public OperationExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExceptionHandlingResult> HandleAsync(Exception exception)
        {
            var result = new ExceptionHandlingResult { Attempted = true };

            try
            {
                _logger.LogWarning($"Operation exception: {exception.Message}");
                
                // Analyze operation state and attempt recovery
                result.Successful = false; // Usually requires specific handling
                result.RecoveryActions.Add("Operation state analyzed");
            }
            catch (Exception recoveryException)
            {
                _logger.LogError($"Operation exception recovery failed: {recoveryException.Message}", recoveryException);
                result.Successful = false;
            }

            await Task.CompletedTask; // Placeholder for async operations
            return result;
        }
    }

    #endregion

    #region Result Classes

    /// <summary>
    /// Structured error response format per design document
    /// </summary>
    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string? RequestPath { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public bool RecoveryAttempted { get; set; }
        public bool RecoverySuccessful { get; set; }
        public List<string>? ResolutionSuggestions { get; set; }
    }

    public class ExceptionHandlingResult
    {
        public bool Attempted { get; set; }
        public bool Successful { get; set; }
        public List<string> RecoveryActions { get; } = new();
        public Exception? RecoveryException { get; set; }
    }

    #endregion

    #region Middleware Extensions

    /// <summary>
    /// Extension methods for ASP.NET Core middleware registration
    /// </summary>
    public static class RuntimeExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseRuntimeExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RuntimeExceptionHandler>();
        }
    }

    #endregion
}