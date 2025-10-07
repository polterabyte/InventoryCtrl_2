using System.Net;
using System.Text.Json;
using Inventory.Shared.Services;

namespace Inventory.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var requestId = context.TraceIdentifier;
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var clientIp = GetClientIpAddress(context);
            
            _logger.LogError(ex, "Unhandled exception occurred. RequestId: {RequestId}, Method: {Method}, Path: {Path}, ClientIP: {ClientIP}, UserAgent: {UserAgent}", 
                requestId, context.Request.Method, context.Request.Path, clientIp, userAgent);
            
            // Add to debug logs if available
            try
            {
                var debugLogsService = context.RequestServices.GetService<IDebugLogsService>();
                if (debugLogsService != null)
                {
                    await debugLogsService.AddLogAsync(new LogEntry
                    {
                        Level = Inventory.Shared.Services.LogLevel.Error,
                        Message = $"Unhandled exception: {ex.Message}",
                        Exception = ex.ToString(),
                        Source = "GlobalExceptionMiddleware",
                        Properties = new Dictionary<string, object>
                        {
                            ["RequestId"] = requestId,
                            ["Method"] = context.Request.Method,
                            ["Path"] = context.Request.Path.Value ?? "",
                            ["ClientIP"] = clientIp,
                            ["UserAgent"] = userAgent,
                            ["QueryString"] = context.Request.QueryString.Value ?? ""
                        }
                    });
                }
            }
            catch
            {
                // Don't let debug logging errors break the main flow
            }
            
            await HandleExceptionAsync(context, ex, requestId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
    {
        context.Response.ContentType = "application/json";
        
        var (statusCode, userMessage, technicalMessage) = GetErrorDetails(exception);
        
        var response = new
        {
            error = new
            {
                message = userMessage,
                details = technicalMessage,
                requestId = requestId,
                timestamp = DateTime.UtcNow,
                type = exception.GetType().Name
            }
        };

        context.Response.StatusCode = (int)statusCode;

        var jsonResponse = JsonSerializer.Serialize(response, DefaultJsonOptions);

        await context.Response.WriteAsync(jsonResponse);
    }

    private static (HttpStatusCode statusCode, string userMessage, string technicalMessage) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            ArgumentException or ArgumentNullException => (
                HttpStatusCode.BadRequest,
                "Invalid request parameters. Please check your input and try again.",
                exception.Message
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "You are not authorized to perform this action.",
                exception.Message
            ),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "The requested resource was not found.",
                exception.Message
            ),
            InvalidOperationException => (
                HttpStatusCode.Conflict,
                "The operation cannot be completed in the current state.",
                exception.Message
            ),
            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                "The request timed out. Please try again.",
                exception.Message
            ),
            HttpRequestException => (
                HttpStatusCode.BadGateway,
                "Unable to connect to external service. Please try again later.",
                exception.Message
            ),
            NotImplementedException => (
                HttpStatusCode.NotImplemented,
                "This feature is not yet implemented.",
                exception.Message
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again or contact support if the problem persists.",
                "An internal server error occurred."
            )
        };
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
