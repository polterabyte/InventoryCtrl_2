using System.Diagnostics;
using Inventory.API.Services;
using Inventory.API.Enums;

namespace Inventory.API.Middleware;

/// <summary>
/// Middleware for automatic HTTP request auditing
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AuditService auditService)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;
        var requestId = Guid.NewGuid().ToString();

        try
        {
            // Skip auditing for certain paths
            if (ShouldSkipAuditing(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Add request ID to response headers for tracing
            context.Response.Headers.Add("X-Request-ID", requestId);

            // Log the request start
            _logger.LogDebug("Auditing request: {Method} {Path} [RequestId: {RequestId}]", 
                context.Request.Method, context.Request.Path, requestId);

            // Capture request details
            var requestMethod = context.Request.Method;
            var requestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = GetClientIpAddress(context);
            
            // Determine action type based on HTTP method
            var actionType = GetActionTypeFromHttpMethod(requestMethod);
            var entityType = GetEntityTypeFromPath(context.Request.Path);

            // Create a new response body stream to capture the response
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            // Process the request
            await _next(context);

            stopwatch.Stop();

            // Log successful request with enhanced details
            await auditService.LogDetailedChangeAsync(
                "HTTP",
                requestId,
                $"HTTP {requestMethod}",
                actionType,
                entityType,
                new
                {
                    Method = requestMethod,
                    Url = requestUrl,
                    StatusCode = context.Response.StatusCode,
                    Duration = stopwatch.ElapsedMilliseconds,
                    UserAgent = userAgent,
                    IpAddress = ipAddress,
                    QueryString = context.Request.QueryString.ToString(),
                    Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
                },
                requestId,
                $"HTTP {requestMethod} request to {context.Request.Path}",
                context.Response.StatusCode < 400 ? "INFO" : "WARNING",
                context.Response.StatusCode < 400,
                context.Response.StatusCode >= 400 ? $"HTTP {context.Response.StatusCode}" : null);

            // Copy the response back to the original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log failed request with enhanced details
            await auditService.LogDetailedChangeAsync(
                "HTTP",
                requestId,
                $"HTTP {context.Request.Method}",
                GetActionTypeFromHttpMethod(context.Request.Method),
                GetEntityTypeFromPath(context.Request.Path),
                new
                {
                    Method = context.Request.Method,
                    Url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
                    StatusCode = 500,
                    Duration = stopwatch.ElapsedMilliseconds,
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    IpAddress = GetClientIpAddress(context),
                    QueryString = context.Request.QueryString.ToString(),
                    Exception = ex.GetType().Name,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace
                },
                requestId,
                $"HTTP {context.Request.Method} request to {context.Request.Path} failed",
                "ERROR",
                false,
                ex.Message);

            _logger.LogError(ex, "Error processing request: {Method} {Path} [RequestId: {RequestId}]", 
                context.Request.Method, context.Request.Path, requestId);

            // Re-throw the exception
            throw;
        }
        finally
        {
            // Ensure the original response body stream is restored
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldSkipAuditing(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        
        // Skip auditing for these paths
        var skipPaths = new[]
        {
            "/swagger",
            "/health",
            "/favicon.ico",
            "/_blazor",
            "/css",
            "/js",
            "/images",
            "/lib"
        };

        return skipPaths.Any(skipPath => pathValue.StartsWith(skipPath));
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Determines ActionType based on HTTP method
    /// </summary>
    private static ActionType GetActionTypeFromHttpMethod(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => ActionType.Read,
            "POST" => ActionType.Create,
            "PUT" or "PATCH" => ActionType.Update,
            "DELETE" => ActionType.Delete,
            _ => ActionType.Other
        };
    }

    /// <summary>
    /// Determines entity type based on request path
    /// </summary>
    private static string GetEntityTypeFromPath(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        
        if (pathValue.StartsWith("/api/"))
        {
            var segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                return segments[1].ToUpperInvariant(); // Return controller name
            }
        }
        
        if (pathValue.StartsWith("/swagger"))
            return "Swagger";
            
        if (pathValue.StartsWith("/health"))
            return "Health";
            
        return "HTTP";
    }
}
