using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

/// <summary>
/// Generic interface for reference data services
/// </summary>
/// <typeparam name="TDto">DTO type</typeparam>
/// <typeparam name="TCreateDto">Create DTO type</typeparam>
/// <typeparam name="TUpdateDto">Update DTO type</typeparam>
public interface IReferenceDataService<TDto, TCreateDto, TUpdateDto>
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    /// <summary>
    /// Get all reference data items with pagination and filtering
    /// </summary>
    Task<PagedApiResponse<TDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, bool? isActive = null);

    /// <summary>
    /// Get all reference data items (for dropdowns, etc.)
    /// </summary>
    Task<ApiResponse<List<TDto>>> GetAllSimpleAsync();

    /// <summary>
    /// Get reference data item by ID
    /// </summary>
    Task<ApiResponse<TDto>> GetByIdAsync(int id);

    /// <summary>
    /// Create new reference data item
    /// </summary>
    Task<ApiResponse<TDto>> CreateAsync(TCreateDto createDto);

    /// <summary>
    /// Update reference data item
    /// </summary>
    Task<ApiResponse<TDto>> UpdateAsync(int id, TUpdateDto updateDto);

    /// <summary>
    /// Delete reference data item (soft delete)
    /// </summary>
    Task<ApiResponse<object>> DeleteAsync(int id);

    /// <summary>
    /// Check if item exists by name/symbol
    /// </summary>
    Task<bool> ExistsAsync(string identifier);

    /// <summary>
    /// Get items count
    /// </summary>
    Task<int> GetCountAsync(bool? isActive = null);
}
