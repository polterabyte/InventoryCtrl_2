using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.DTOs
{
    /// <summary>
    /// SSL Certificate Data Transfer Object
    /// </summary>
    public class SSLCertificateDto
    {
        /// <summary>
        /// Certificate ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Domain name
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Certificate file path
        /// </summary>
        public string CertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// Private key file path
        /// </summary>
        public string PrivateKeyPath { get; set; } = string.Empty;

        /// <summary>
        /// Certificate issuer
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Certificate subject
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Certificate serial number
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Certificate thumbprint
        /// </summary>
        public string Thumbprint { get; set; } = string.Empty;

        /// <summary>
        /// Valid from date
        /// </summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// Valid to date
        /// </summary>
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// Days until expiration
        /// </summary>
        public int DaysUntilExpiration { get; set; }

        /// <summary>
        /// Is certificate valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Is certificate expired
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// Is certificate expiring soon (within 30 days)
        /// </summary>
        public bool IsExpiringSoon { get; set; }

        /// <summary>
        /// Certificate type (SelfSigned, LetsEncrypt, Commercial)
        /// </summary>
        public string CertificateType { get; set; } = string.Empty;

        /// <summary>
        /// Key size in bits
        /// </summary>
        public int KeySize { get; set; }

        /// <summary>
        /// Subject Alternative Names
        /// </summary>
        public string[] SubjectAlternativeNames { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Environment (development, staging, production)
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Created date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last updated date
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Certificate status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Error message if any
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// SSL Certificate Health Data Transfer Object
    /// </summary>
    public class SSLCertificateHealthDto
    {
        /// <summary>
        /// Total number of certificates
        /// </summary>
        public int TotalCertificates { get; set; }

        /// <summary>
        /// Number of valid certificates
        /// </summary>
        public int ValidCertificates { get; set; }

        /// <summary>
        /// Number of expired certificates
        /// </summary>
        public int ExpiredCertificates { get; set; }

        /// <summary>
        /// Number of certificates expiring soon
        /// </summary>
        public int ExpiringSoonCertificates { get; set; }

        /// <summary>
        /// Number of invalid certificates
        /// </summary>
        public int InvalidCertificates { get; set; }

        /// <summary>
        /// Overall health status
        /// </summary>
        public string OverallStatus { get; set; } = string.Empty;

        /// <summary>
        /// Health score (0-100)
        /// </summary>
        public int HealthScore { get; set; }

        /// <summary>
        /// List of issues found
        /// </summary>
        public string[] Issues { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Recommendations
        /// </summary>
        public string[] Recommendations { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Last health check date
        /// </summary>
        public DateTime LastHealthCheck { get; set; }
    }

    /// <summary>
    /// SSL Certificate Validation Data Transfer Object
    /// </summary>
    public class SSLCertificateValidationDto
    {
        /// <summary>
        /// Domain name
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Is certificate valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Validation details
        /// </summary>
        public string[] ValidationDetails { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Validation errors
        /// </summary>
        public string[] ValidationErrors { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Certificate chain validation
        /// </summary>
        public bool ChainValid { get; set; }

        /// <summary>
        /// Certificate signature validation
        /// </summary>
        public bool SignatureValid { get; set; }

        /// <summary>
        /// Certificate date validation
        /// </summary>
        public bool DateValid { get; set; }

        /// <summary>
        /// Certificate domain validation
        /// </summary>
        public bool DomainValid { get; set; }

        /// <summary>
        /// Validation timestamp
        /// </summary>
        public DateTime ValidatedAt { get; set; }
    }
}
