using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

/// <summary>
/// Generic базовый класс для API сервисов с CRUD операциями
/// </summary>
/// <typeparam name="TEntity">Тип сущности</typeparam>
/// <typeparam name="TCreateDto">Тип DTO для создания</typeparam>
/// <typeparam name="TUpdateDto">Тип DTO для обновления</typeparam>
public abstract class WebApiServiceBase<TEntity, TCreateDto, TUpdateDto> : WebBaseApiService
    where TEntity : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected WebApiServiceBase(
        HttpClient httpClient, 
        IUrlBuilderService urlBuilderService, 
        IResilientApiService resilientApiService,
        IApiErrorHandler errorHandler,
        IRequestValidator requestValidator,
        ILogger logger) 
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, logger)
    {
    }

    /// <summary>
    /// Базовый endpoint для сущности (например, "/api/manufacturers")
    /// </summary>
    protected abstract string BaseEndpoint { get; }

    /// <summary>
    /// Получить все сущности
    /// </summary>
    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        var response = await GetAsync<List<TEntity>>(BaseEndpoint);
        return response?.Data ?? new List<TEntity>();
    }

    /// <summary>
    /// Получить сущность по ID
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(int id)
    {
        var response = await GetAsync<TEntity>($"{BaseEndpoint}/{id}");
        return response?.Data;
    }

    /// <summary>
    /// Создать новую сущность
    /// </summary>
    public virtual async Task<TEntity> CreateAsync(TCreateDto createDto)
    {
        var response = await PostAsync<TEntity>(BaseEndpoint, createDto);
        return response?.Data ?? throw new InvalidOperationException("Failed to create entity");
    }

    /// <summary>
    /// Обновить существующую сущность
    /// </summary>
    public virtual async Task<TEntity?> UpdateAsync(int id, TUpdateDto updateDto)
    {
        var response = await PutAsync<TEntity>($"{BaseEndpoint}/{id}", updateDto);
        return response?.Data;
    }

    /// <summary>
    /// Удалить сущность по ID
    /// </summary>
    public virtual async Task<bool> DeleteAsync(int id)
    {
        var response = await DeleteAsync($"{BaseEndpoint}/{id}");
        return response?.Data ?? false;
    }

    /// <summary>
    /// Получить сущности с пагинацией
    /// </summary>
    public virtual async Task<PagedApiResponse<TEntity>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var queryParams = new List<string>();
        
        if (page > 1) queryParams.Add($"page={page}");
        if (pageSize != 10) queryParams.Add($"pageSize={pageSize}");
        if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        
        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var endpoint = $"{BaseEndpoint}{queryString}";
        
        return await GetPagedAsync<TEntity>(endpoint);
    }

    /// <summary>
    /// Получить сущности с пагинацией (перегрузка для кастомного endpoint)
    /// </summary>
    protected virtual async Task<PagedApiResponse<TEntity>> GetPagedAsync(string endpoint)
    {
        return await GetPagedAsync<TEntity>(endpoint);
    }
}
