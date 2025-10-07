using Inventory.Shared.DTOs;

namespace Inventory.Shared.Interfaces
{
    /// <summary>
    /// Interface for SSL Certificate management service
    /// </summary>
    public interface ISSLCertificateService
    {
        /// <summary>
        /// Get all SSL certificates
        /// </summary>
        /// <returns>List of SSL certificates</returns>
        Task<IEnumerable<SSLCertificateDto>> GetAllCertificatesAsync();

        /// <summary>
        /// Get SSL certificate by domain
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>SSL certificate details or null if not found</returns>
        Task<SSLCertificateDto?> GetCertificateByDomainAsync(string domain);

        /// <summary>
        /// Generate new SSL certificate
        /// </summary>
        /// <param name="request">Certificate generation request</param>
        /// <returns>Generated certificate details</returns>
        Task<SSLCertificateDto> GenerateCertificateAsync(GenerateCertificateRequest request);

        /// <summary>
        /// Renew SSL certificate
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>Renewed certificate details or null if not found</returns>
        Task<SSLCertificateDto?> RenewCertificateAsync(string domain);

        /// <summary>
        /// Delete SSL certificate
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>True if deleted successfully, false if not found</returns>
        Task<bool> DeleteCertificateAsync(string domain);

        /// <summary>
        /// Get certificate health status
        /// </summary>
        /// <returns>Certificate health information</returns>
        Task<SSLCertificateHealthDto> GetCertificateHealthAsync();

        /// <summary>
        /// Validate certificate for domain
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>Validation result or null if certificate not found</returns>
        Task<SSLCertificateValidationDto?> ValidateCertificateAsync(string domain);

        /// <summary>
        /// Check if certificate exists for domain
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>True if certificate exists</returns>
        Task<bool> CertificateExistsAsync(string domain);

        /// <summary>
        /// Get certificates expiring soon
        /// </summary>
        /// <param name="days">Number of days to check (default 30)</param>
        /// <returns>List of certificates expiring soon</returns>
        Task<IEnumerable<SSLCertificateDto>> GetCertificatesExpiringSoonAsync(int days = 30);

        /// <summary>
        /// Refresh certificate information from files
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>Updated certificate details or null if not found</returns>
        Task<SSLCertificateDto?> RefreshCertificateInfoAsync(string domain);
    }

    /// <summary>
    /// Request model for generating SSL certificates
    /// </summary>
    public class GenerateCertificateRequest
    {
        public string Domain { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool UseLetsEncrypt { get; set; }
        public int KeySize { get; set; } = 4096;
        public int ValidityDays { get; set; } = 365;
        public string[]? SubjectAlternativeNames { get; set; }
        public string Environment { get; set; } = "development";
    }
}
