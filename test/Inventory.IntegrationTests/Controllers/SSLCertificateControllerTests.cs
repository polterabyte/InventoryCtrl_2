using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Moq;

namespace Inventory.IntegrationTests.Controllers
{
    public class SSLCertificateControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private Mock<ISSLCertificateService> _mockSslService;

        public SSLCertificateControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real SSL service
                    var sslServiceDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ISSLCertificateService));
                    if (sslServiceDescriptor != null)
                    {
                        services.Remove(sslServiceDescriptor);
                    }

                    // Add mock SSL service
                    _mockSslService = new Mock<ISSLCertificateService>();
                    services.AddScoped<ISSLCertificateService>(_ => _mockSslService.Object);
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetCertificates_ShouldReturnOk_WhenCertificatesExist()
        {
            // Arrange
            var certificates = new List<SSLCertificateDto>
            {
                new SSLCertificateDto
                {
                    Id = 1,
                    Domain = "test.com",
                    CertificatePath = "/path/to/cert.crt",
                    PrivateKeyPath = "/path/to/key.key",
                    IsValid = true,
                    Status = "Active"
                }
            };

            _mockSslService.Setup(x => x.GetAllCertificatesAsync())
                .ReturnsAsync(certificates);

            // Act
            var response = await _client.GetAsync("/api/SSLCertificate");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<SSLCertificateDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test.com", result[0].Domain);
        }

        [Fact]
        public async Task GetCertificate_ShouldReturnOk_WhenCertificateExists()
        {
            // Arrange
            var domain = "test.com";
            var certificate = new SSLCertificateDto
            {
                Id = 1,
                Domain = domain,
                CertificatePath = "/path/to/cert.crt",
                PrivateKeyPath = "/path/to/key.key",
                IsValid = true,
                Status = "Active"
            };

            _mockSslService.Setup(x => x.GetCertificateByDomainAsync(domain))
                .ReturnsAsync(certificate);

            // Act
            var response = await _client.GetAsync($"/api/SSLCertificate/{domain}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SSLCertificateDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(domain, result.Domain);
        }

        [Fact]
        public async Task GetCertificate_ShouldReturnNotFound_WhenCertificateDoesNotExist()
        {
            // Arrange
            var domain = "nonexistent.com";

            _mockSslService.Setup(x => x.GetCertificateByDomainAsync(domain))
                .ReturnsAsync((SSLCertificateDto?)null);

            // Act
            var response = await _client.GetAsync($"/api/SSLCertificate/{domain}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GenerateCertificate_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var request = new GenerateCertificateRequest
            {
                Domain = "test.com",
                UseLetsEncrypt = false,
                KeySize = 2048,
                ValidityDays = 365
            };

            var certificate = new SSLCertificateDto
            {
                Id = 1,
                Domain = request.Domain,
                CertificatePath = "/path/to/cert.crt",
                PrivateKeyPath = "/path/to/key.key",
                IsValid = true,
                Status = "Active"
            };

            _mockSslService.Setup(x => x.GenerateCertificateAsync(It.IsAny<GenerateCertificateRequest>()))
                .ReturnsAsync(certificate);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/SSLCertificate/generate", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SSLCertificateDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(request.Domain, result.Domain);
        }

        [Fact]
        public async Task GenerateCertificate_ShouldReturnBadRequest_WhenRequestIsInvalid()
        {
            // Arrange
            var request = new GenerateCertificateRequest
            {
                Domain = "", // Invalid empty domain
                UseLetsEncrypt = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/SSLCertificate/generate", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RenewCertificate_ShouldReturnOk_WhenCertificateExists()
        {
            // Arrange
            var domain = "test.com";
            var certificate = new SSLCertificateDto
            {
                Id = 1,
                Domain = domain,
                CertificatePath = "/path/to/cert.crt",
                PrivateKeyPath = "/path/to/key.key",
                IsValid = true,
                Status = "Active"
            };

            _mockSslService.Setup(x => x.RenewCertificateAsync(domain))
                .ReturnsAsync(certificate);

            // Act
            var response = await _client.PostAsync($"/api/SSLCertificate/{domain}/renew", null);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SSLCertificateDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(domain, result.Domain);
        }

        [Fact]
        public async Task RenewCertificate_ShouldReturnNotFound_WhenCertificateDoesNotExist()
        {
            // Arrange
            var domain = "nonexistent.com";

            _mockSslService.Setup(x => x.RenewCertificateAsync(domain))
                .ReturnsAsync((SSLCertificateDto?)null);

            // Act
            var response = await _client.PostAsync($"/api/SSLCertificate/{domain}/renew", null);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCertificate_ShouldReturnNoContent_WhenCertificateExists()
        {
            // Arrange
            var domain = "test.com";

            _mockSslService.Setup(x => x.DeleteCertificateAsync(domain))
                .ReturnsAsync(true);

            // Act
            var response = await _client.DeleteAsync($"/api/SSLCertificate/{domain}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCertificate_ShouldReturnNotFound_WhenCertificateDoesNotExist()
        {
            // Arrange
            var domain = "nonexistent.com";

            _mockSslService.Setup(x => x.DeleteCertificateAsync(domain))
                .ReturnsAsync(false);

            // Act
            var response = await _client.DeleteAsync($"/api/SSLCertificate/{domain}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCertificateHealth_ShouldReturnOk()
        {
            // Arrange
            var health = new SSLCertificateHealthDto
            {
                TotalCertificates = 5,
                ValidCertificates = 4,
                ExpiredCertificates = 1,
                ExpiringSoonCertificates = 0,
                InvalidCertificates = 0,
                OverallStatus = "Good",
                HealthScore = 80,
                LastHealthCheck = DateTime.UtcNow
            };

            _mockSslService.Setup(x => x.GetCertificateHealthAsync())
                .ReturnsAsync(health);

            // Act
            var response = await _client.GetAsync("/api/SSLCertificate/health");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SSLCertificateHealthDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(5, result.TotalCertificates);
            Assert.Equal(4, result.ValidCertificates);
            Assert.Equal("Good", result.OverallStatus);
        }

        [Fact]
        public async Task ValidateCertificate_ShouldReturnOk_WhenCertificateExists()
        {
            // Arrange
            var domain = "test.com";
            var validation = new SSLCertificateValidationDto
            {
                Domain = domain,
                IsValid = true,
                Status = "Valid",
                ChainValid = true,
                SignatureValid = true,
                DateValid = true,
                DomainValid = true,
                ValidatedAt = DateTime.UtcNow
            };

            _mockSslService.Setup(x => x.ValidateCertificateAsync(domain))
                .ReturnsAsync(validation);

            // Act
            var response = await _client.PostAsync($"/api/SSLCertificate/{domain}/validate", null);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SSLCertificateValidationDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(domain, result.Domain);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ValidateCertificate_ShouldReturnNotFound_WhenCertificateDoesNotExist()
        {
            // Arrange
            var domain = "nonexistent.com";

            _mockSslService.Setup(x => x.ValidateCertificateAsync(domain))
                .ReturnsAsync((SSLCertificateValidationDto?)null);

            // Act
            var response = await _client.PostAsync($"/api/SSLCertificate/{domain}/validate", null);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        public void Dispose()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }
    }
}
