using System.Collections.Generic;

namespace Inventory.Shared.DTOs
{
    /// <summary>
    /// A generic API response wrapper.
    /// </summary>
    /// <typeparam name="T">The type of the data being returned.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates whether the request was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The data payload of the response.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// An error message if the request was not successful.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// A list of validation errors, if any.
        /// </summary>
        public List<string>? ValidationErrors { get; set; }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        /// <param name="data">The data to include in the response.</param>
        /// <returns>A successful ApiResponse.</returns>
        public static ApiResponse<T> SuccessResult(T data)
        {
            return new ApiResponse<T> { Success = true, Data = data };
        }

        /// <summary>
        /// Creates a failed API response.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="validationErrors">A list of validation errors.</param>
        /// <returns>An error ApiResponse.</returns>
        public static ApiResponse<T> ErrorResult(string errorMessage, List<string>? validationErrors = null)
        {
            return new ApiResponse<T> { Success = false, ErrorMessage = errorMessage, ValidationErrors = validationErrors };
        }
    }
}
