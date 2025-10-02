using Inventory.Shared.Constants;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Inventory.Web.Client.Services;

/// <summary>
/// API сервис для работы с производителями
/// </summary>
public class WebManufacturerApiService : WebApiServiceBase<ManufacturerDto, CreateManufacturerDto, UpdateManufacturerDto>, IManufacturerService
{
    public WebManufacturerApiService(
        HttpClient httpClient, 
        IUrlBuilderService urlBuilderService, 
        IResilientApiService resilientApiService, 
        IApiErrorHandler errorHandler, 
        IRequestValidator requestValidator,
        IAutoTokenRefreshService autoTokenRefreshService,
        ILogger<WebManufacturerApiService> logger) 
        : base(httpClient, urlBuilderService, resilientApiService, errorHandler, requestValidator, autoTokenRefreshService, logger)
    {
    }

    protected override string BaseEndpoint => ApiEndpoints.Manufacturers;

    // Реализация интерфейса IManufacturerService через базовые методы
    public async Task<List<ManufacturerDto>> GetAllManufacturersAsync() => await GetAllAsync();
    public async Task<ManufacturerDto?> GetManufacturerByIdAsync(int id) => await GetByIdAsync(id);
    public async Task<ManufacturerDto> CreateManufacturerAsync(CreateManufacturerDto createManufacturerDto) => await CreateAsync(createManufacturerDto);
    public async Task<ManufacturerDto?> UpdateManufacturerAsync(int id, UpdateManufacturerDto updateManufacturerDto) => await UpdateAsync(id, updateManufacturerDto);
    public async Task<bool> DeleteManufacturerAsync(int id) => await DeleteAsync(id);
}
