using System.Collections.Generic;

namespace Inventory.Shared.DTOs
{
    /// <summary>
    /// API ответ с пагинированными данными
    /// </summary>
    public class PagedApiResponse<T>
    {
        public bool Success { get; set; }
        public PagedResponse<T>? Data { get; set; }
        public string? Error { get; set; }
        public List<string>? ValidationErrors { get; set; }

        /// <summary>
        /// Создает успешный пагинированный ответ
        /// </summary>
        public static PagedApiResponse<T> CreateSuccess(PagedResponse<T> data)
        {
            return new PagedApiResponse<T>
            {
                Success = true,
                Data = data
            };
        }

        /// <summary>
        /// Создает пагинированный ответ с ошибкой
        /// </summary>
        public static PagedApiResponse<T> CreateFailure(string error, List<string>? validationErrors = null)
        {
            return new PagedApiResponse<T>
            {
                Success = false,
                Error = error,
                ValidationErrors = validationErrors
            };
        }
    }

    /// <summary>
    /// Пагинированный ответ
    /// </summary>
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int total { get; set; }
        public int page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling((double)total / PageSize) : 0;
        public bool HasPreviousPage => page > 1;
        public bool HasNextPage => page < TotalPages;
    }
}
