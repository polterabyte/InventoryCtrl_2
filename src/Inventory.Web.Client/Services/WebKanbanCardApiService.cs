using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

public class WebKanbanCardApiService : WebBaseApiService, IKanbanCardService
{
    public WebKanbanCardApiService(
        HttpClient httpClient,
        IUrlBuilderService urlBuilderService,
        IResilientApiService resilientApiService,
        IApiErrorHandler errorHandler,
        IRequestValidator requestValidator,
        ILogger<WebKanbanCardApiService> logger)
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, logger)
    {
    }

    public async Task<List<KanbanCardDto>> GetAllAsync(int? productId = null, int? warehouseId = null)
    {
        var query = new List<string>();
        if (productId.HasValue) query.Add($"productId={productId.Value}");
        if (warehouseId.HasValue) query.Add($"warehouseId={warehouseId.Value}");
        var endpoint = ApiEndpoints.KanbanCards + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        var response = await GetAsync<List<KanbanCardDto>>(endpoint);
        return response.Data ?? new List<KanbanCardDto>();
    }

    public async Task<KanbanCardDto?> GetByIdAsync(int id)
    {
        var endpoint = ApiEndpoints.KanbanCardById.Replace("{id}", id.ToString());
        var response = await GetAsync<KanbanCardDto>(endpoint);
        return response.Data;
    }

    public async Task<KanbanCardDto> CreateAsync(CreateKanbanCardDto dto)
    {
        var response = await PostAsync<KanbanCardDto>(ApiEndpoints.KanbanCards, dto);
        return response.Data ?? new KanbanCardDto();
    }

    public async Task<KanbanCardDto?> UpdateAsync(int id, UpdateKanbanCardDto dto)
    {
        var endpoint = ApiEndpoints.KanbanCardById.Replace("{id}", id.ToString());
        var response = await PutAsync<KanbanCardDto>(endpoint, dto);
        return response.Data;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var endpoint = ApiEndpoints.KanbanCardById.Replace("{id}", id.ToString());
        var response = await base.DeleteAsync(endpoint);
        return response.Success && response.Data;
    }

    public async Task<bool> ReassignAsync(int cardId, int newWarehouseId)
    {
        var endpoint = $"{ApiEndpoints.KanbanCardById.Replace("{id}", cardId.ToString())}/reassign";
        var dto = new { NewWarehouseId = newWarehouseId };
        var response = await PutAsync<bool>(endpoint, dto);
        return response.Success && response.Data;
    }
}
