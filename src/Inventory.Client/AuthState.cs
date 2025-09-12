using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Inventory.Client
{
    public class AuthState
    {
        private readonly IJSRuntime _jsRuntime;
        private const string TOKEN_KEY = "authToken";

        public AuthState(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string> GetTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TOKEN_KEY);
        }

        public async Task SetTokenAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
        }

        public async Task RemoveTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
        }
    }
}