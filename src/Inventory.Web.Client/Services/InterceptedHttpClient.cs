using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

/// <summary>
/// HttpClient с поддержкой перехватчиков
/// </summary>
public class InterceptedHttpClient : HttpClient
{
    private readonly IHttpInterceptor _interceptor;
    private readonly ILogger<InterceptedHttpClient> _logger;

    public InterceptedHttpClient(
        IHttpInterceptor interceptor,
        ILogger<InterceptedHttpClient> logger)
    {
        _interceptor = interceptor;
        _logger = logger;
    }

    public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        return await _interceptor.InterceptAsync(request, async () =>
        {
            return await base.SendAsync(request, cancellationToken);
        });
    }
}
