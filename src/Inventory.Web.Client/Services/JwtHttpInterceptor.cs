using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

/// <summary>
/// HTTP Interceptor для автоматической проверки и обновления JWT токенов
/// </summary>
public class JwtHttpInterceptor : DelegatingHandler
{
    private readonly ITokenManagementService _tokenManagementService;
    private readonly ILogger<JwtHttpInterceptor> _logger;

    public JwtHttpInterceptor(
        ITokenManagementService tokenManagementService,
        ILogger<JwtHttpInterceptor> logger)
    {
        _tokenManagementService = tokenManagementService;
        _logger = logger;
    }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("JwtHttpInterceptor: Sending request to {Url}", request.RequestUri);

        var token = await _tokenManagementService.GetStoredTokenAsync();
        _logger.LogInformation("JwtHttpInterceptor: Retrieved token: {HasToken}", !string.IsNullOrEmpty(token));
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation("JwtHttpInterceptor: Added Authorization header to request");
        }
        else
        {
            _logger.LogWarning("JwtHttpInterceptor: No token available for request");
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("JwtHttpInterceptor: Unauthorized response. Attempting to refresh token.");

            var refreshed = await _tokenManagementService.TryRefreshTokenAsync(forceRefresh: true);
            if (refreshed)
            {
                _logger.LogInformation("JwtHttpInterceptor: Token refreshed successfully. Retrying request with new token.");

                // Clone the request and add the new token
                var retryRequest = await CloneHttpRequestMessageAsync(request);
                var newToken = await _tokenManagementService.GetStoredTokenAsync();
                if (!string.IsNullOrEmpty(newToken))
                {
                    retryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
                }

                // Retry the request with the new token
                response = await base.SendAsync(retryRequest, cancellationToken);
                _logger.LogDebug("JwtHttpInterceptor: Retry completed with status {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogWarning("JwtHttpInterceptor: Failed to refresh token. Unauthorized request will proceed.");
            }
        }

        return response;
    }

    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            if (req.Content.Headers != null)
            {
                foreach (var h in req.Content.Headers)
                {
                    clone.Content.Headers.Add(h.Key, h.Value);
                }
            }
        }

        clone.Version = req.Version;

        foreach (var prop in req.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key), prop.Value);
        }

        foreach (var header in req.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
