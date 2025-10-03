using Inventory.API.Models;
using System.Threading.Tasks;

namespace Inventory.API.Services
{
    public interface IRefreshTokenService
    {
        string GenerateRefreshToken();
        Task SetRefreshTokenAsync(User user, string refreshToken);
        bool ValidateRefreshToken(User user, string refreshToken);
        Task RevokeRefreshTokenAsync(User user);
        Task RevokeRefreshTokenAsync(string username);
        Task<int> CleanupExpiredTokensAsync();
        Task<string> GenerateAccessTokenAsync(User user);
    }
}
