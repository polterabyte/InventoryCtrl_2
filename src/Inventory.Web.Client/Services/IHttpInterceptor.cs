using System.Net.Http;
using System.Threading.Tasks;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Интерфейс для перехвата HTTP запросов
/// </summary>
public interface IHttpInterceptor
{
    /// <summary>
    /// Перехватывает HTTP запрос и выполняет дополнительную логику
    /// </summary>
    /// <param name="request">HTTP запрос</param>
    /// <param name="next">Следующий обработчик в цепочке</param>
    /// <returns>HTTP ответ</returns>
    Task<HttpResponseMessage> InterceptAsync(HttpRequestMessage request, Func<Task<HttpResponseMessage>> next);
}
