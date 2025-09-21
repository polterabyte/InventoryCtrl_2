using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces;

public interface IUserManagementService
{
    Task<PagedApiResponse<UserDto>?> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null, string? role = null);
    Task<ApiResponse<UserDto>?> GetUserAsync(string id);
    Task<ApiResponse<UserDto>?> CreateUserAsync(CreateUserDto user);
    Task<ApiResponse<UserDto>?> UpdateUserAsync(string id, UpdateUserDto user);
    Task<ApiResponse<object>?> DeleteUserAsync(string id);
    Task<ApiResponse<object>?> ChangePasswordAsync(string id, ChangePasswordDto passwordDto);
    Task<bool> ExportUsersAsync(string? search = null, string? role = null);
}
