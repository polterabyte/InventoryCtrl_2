using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Inventory.API.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Пропускаем проверку для публичных эндпоинтов
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Пропускаем проверку для статических файлов
        if (IsStaticFile(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Проверяем наличие токена в заголовке Authorization
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Missing or invalid Authorization header for path: {Path}", context.Request.Path);
                await HandleUnauthorized(context);
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            // Проверяем валидность токена
            if (!await ValidateTokenAsync(token))
            {
                _logger.LogWarning("Invalid token for path: {Path}", context.Request.Path);
                await HandleUnauthorized(context);
                return;
            }

            // Токен валиден, продолжаем обработку
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AuthenticationMiddleware for path: {Path}", context.Request.Path);
            await HandleUnauthorized(context);
        }
    }

    private bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/health",
            "/health",
            "/swagger",
            "/notificationHub"
        };

        return publicPaths.Any(publicPath => path.StartsWithSegments(publicPath));
    }

    private bool IsStaticFile(PathString path)
    {
        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
        return staticExtensions.Any(ext => path.Value?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true);
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT settings are not properly configured");
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.UTF8.GetBytes(key);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            // Проверяем, что токен не истек
            if (validatedToken is JwtSecurityToken jwtToken)
            {
                var exp = jwtToken.Claims.FirstOrDefault(x => x.Type == "exp")?.Value;
                if (!string.IsNullOrEmpty(exp) && long.TryParse(exp, out var expUnix))
                {
                    var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                    if (expDateTime <= DateTimeOffset.UtcNow)
                    {
                        _logger.LogWarning("Token has expired");
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return false;
        }
    }

    private async Task HandleUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            success = false,
            message = "Unauthorized access. Please log in.",
            error = "UNAUTHORIZED"
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}
