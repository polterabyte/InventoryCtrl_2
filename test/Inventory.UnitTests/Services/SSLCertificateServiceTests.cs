using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using System.Security.Cryptography.X509Certificates;

namespace Inventory.UnitTests.Services
{
    public class SSLCertificateServiceTests : TestBase
    {
        private readonly Mock<ILogger<SSLCertificateService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly SSLCertificateService _sslService;

        public SSLCertificateServiceTests()
        {
            _mockLogger = new Mock<ILogger<SSLCertificateService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup configuration
            _mockConfiguration.Setup(x => x["SSL:Path"]).Returns("test-ssl");
            
            _sslService = new SSLCertificateService(_mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task GetAllCertificatesAsync_ShouldReturnEmptyList_WhenNoCertificatesExist()
        {
            // Arrange
            // Ensure test directory doesn't exist
            if (Directory.Exists("test-ssl"))
            {
                Directory.Delete("test-ssl", true);
            }

            // Act
            var result = await _sslService.GetAllCertificatesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCertificateByDomainAsync_ShouldReturnNull_WhenCertificateNotFound()
        {
            // Arrange
            var domain = "nonexistent.com";

            // Act
            var result = await _sslService.GetCertificateByDomainAsync(domain);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CertificateExistsAsync_ShouldReturnFalse_WhenCertificateNotFound()
        {
            // Arrange
            var domain = "nonexistent.com";

            // Act
            var result = await _sslService.CertificateExistsAsync(domain);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetCertificateHealthAsync_ShouldReturnHealthInfo_WhenNoCertificates()
        {
            // Act
            var result = await _sslService.GetCertificateHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCertificates);
            Assert.Equal(0, result.ValidCertificates);
            Assert.Equal(0, result.ExpiredCertificates);
            Assert.Equal(0, result.ExpiringSoonCertificates);
            Assert.Equal(0, result.InvalidCertificates);
            Assert.Equal(100, result.HealthScore);
            Assert.Equal("No certificates found", result.OverallStatus);
        }

        [Fact]
        public async Task ValidateCertificateAsync_ShouldReturnNull_WhenCertificateNotFound()
        {
            // Arrange
            var domain = "nonexistent.com";

            // Act
            var result = await _sslService.ValidateCertificateAsync(domain);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCertificatesExpiringSoonAsync_ShouldReturnEmptyList_WhenNoCertificates()
        {
            // Act
            var result = await _sslService.GetCertificatesExpiringSoonAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GenerateCertificateAsync_ShouldThrowArgumentException_WhenDomainIsEmpty()
        {
            // Arrange
            var request = new GenerateCertificateRequest
            {
                Domain = "",
                UseLetsEncrypt = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sslService.GenerateCertificateAsync(request));
        }

        [Fact]
        public async Task GenerateCertificateAsync_ShouldThrowArgumentException_WhenDomainIsNull()
        {
            // Arrange
            var request = new GenerateCertificateRequest
            {
                Domain = null!,
                UseLetsEncrypt = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sslService.GenerateCertificateAsync(request));
        }

        [Fact]
        public async Task GenerateCertificateAsync_ShouldThrowArgumentException_WhenDomainIsWhitespace()
        {
            // Arrange
            var request = new GenerateCertificateRequest
            {
                Domain = "   ",
                UseLetsEncrypt = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sslService.GenerateCertificateAsync(request));
        }

        [Fact]
        public async Task RenewCertificateAsync_ShouldReturnNull_WhenCertificateNotFound()
        {
            // Arrange
            var domain = "nonexistent.com";

            // Act
            var result = await _sslService.RenewCertificateAsync(domain);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteCertificateAsync_ShouldReturnFalse_WhenCertificateNotFound()
        {
            // Arrange
            var domain = "nonexistent.com";

            // Act
            var result = await _sslService.DeleteCertificateAsync(domain);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RefreshCertificateInfoAsync_ShouldReturnNull_WhenCertificateNotFound()
        {
            // Arrange
            var domain = "nonexistent.com";

            // Act
            var result = await _sslService.RefreshCertificateInfoAsync(domain);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(30)]
        [InlineData(60)]
        [InlineData(90)]
        public async Task GetCertificatesExpiringSoonAsync_ShouldReturnEmptyList_WhenNoCertificates(int days)
        {
            // Act
            var result = await _sslService.GetCertificatesExpiringSoonAsync(days);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GenerateCertificateRequest_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var request = new GenerateCertificateRequest();

            // Assert
            Assert.Equal(string.Empty, request.Domain);
            Assert.Null(request.Email);
            Assert.False(request.UseLetsEncrypt);
            Assert.Equal(4096, request.KeySize);
            Assert.Equal(365, request.ValidityDays);
            Assert.Null(request.SubjectAlternativeNames);
            Assert.Equal("development", request.Environment);
        }

        [Fact]
        public void SSLCertificateDto_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new SSLCertificateDto();

            // Assert
            Assert.Equal(0, dto.Id);
            Assert.Equal(string.Empty, dto.Domain);
            Assert.Equal(string.Empty, dto.CertificatePath);
            Assert.Equal(string.Empty, dto.PrivateKeyPath);
            Assert.Equal(string.Empty, dto.Issuer);
            Assert.Equal(string.Empty, dto.Subject);
            Assert.Equal(string.Empty, dto.SerialNumber);
            Assert.Equal(string.Empty, dto.Thumbprint);
            Assert.Equal(DateTime.MinValue, dto.ValidFrom);
            Assert.Equal(DateTime.MinValue, dto.ValidTo);
            Assert.Equal(0, dto.DaysUntilExpiration);
            Assert.False(dto.IsValid);
            Assert.False(dto.IsExpired);
            Assert.False(dto.IsExpiringSoon);
            Assert.Equal(string.Empty, dto.CertificateType);
            Assert.Equal(0, dto.KeySize);
            Assert.Empty(dto.SubjectAlternativeNames);
            Assert.Equal(string.Empty, dto.Environment);
            Assert.Equal(DateTime.MinValue, dto.CreatedAt);
            Assert.Equal(DateTime.MinValue, dto.UpdatedAt);
            Assert.Equal(string.Empty, dto.Status);
            Assert.Null(dto.ErrorMessage);
        }

        [Fact]
        public void SSLCertificateHealthDto_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new SSLCertificateHealthDto();

            // Assert
            Assert.Equal(0, dto.TotalCertificates);
            Assert.Equal(0, dto.ValidCertificates);
            Assert.Equal(0, dto.ExpiredCertificates);
            Assert.Equal(0, dto.ExpiringSoonCertificates);
            Assert.Equal(0, dto.InvalidCertificates);
            Assert.Equal(string.Empty, dto.OverallStatus);
            Assert.Equal(0, dto.HealthScore);
            Assert.Empty(dto.Issues);
            Assert.Empty(dto.Recommendations);
            Assert.Equal(DateTime.MinValue, dto.LastHealthCheck);
        }

        [Fact]
        public void SSLCertificateValidationDto_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var dto = new SSLCertificateValidationDto();

            // Assert
            Assert.Equal(string.Empty, dto.Domain);
            Assert.False(dto.IsValid);
            Assert.Equal(string.Empty, dto.Status);
            Assert.Empty(dto.ValidationDetails);
            Assert.Empty(dto.ValidationErrors);
            Assert.False(dto.ChainValid);
            Assert.False(dto.SignatureValid);
            Assert.False(dto.DateValid);
            Assert.False(dto.DomainValid);
            Assert.Equal(DateTime.MinValue, dto.ValidatedAt);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up test directory
                if (Directory.Exists("test-ssl"))
                {
                    Directory.Delete("test-ssl", true);
                }
            }
            base.Dispose(disposing);
        }
    }
}
