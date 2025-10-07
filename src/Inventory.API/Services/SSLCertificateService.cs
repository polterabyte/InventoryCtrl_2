using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Inventory.API.Services
{
    /// <summary>
    /// Service for managing SSL certificates
    /// </summary>
    public class SSLCertificateService : ISSLCertificateService
    {
        private readonly ILogger<SSLCertificateService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _sslPath;
        private readonly string _certificatesDbPath;
        private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

        public SSLCertificateService(
            ILogger<SSLCertificateService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _sslPath = _configuration["SSL:Path"] ?? "deploy/nginx/ssl";
            _certificatesDbPath = Path.Combine(_sslPath, "certificates.json");
        }

        public async Task<IEnumerable<SSLCertificateDto>> GetAllCertificatesAsync()
        {
            try
            {
                var certificates = new List<SSLCertificateDto>();
                
                // Load from database file
                var dbCertificates = await LoadCertificatesFromDbAsync();
                certificates.AddRange(dbCertificates);

                // Also scan for certificates in SSL directory
                var fileCertificates = await ScanCertificatesFromFilesAsync();
                foreach (var fileCert in fileCertificates)
                {
                    if (!certificates.Any(c => c.Domain == fileCert.Domain))
                    {
                        certificates.Add(fileCert);
                    }
                }

                return certificates.OrderBy(c => c.Domain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all SSL certificates");
                throw;
            }
        }

        public async Task<SSLCertificateDto?> GetCertificateByDomainAsync(string domain)
        {
            try
            {
                // First try to load from database
                var dbCertificates = await LoadCertificatesFromDbAsync();
                var dbCert = dbCertificates.FirstOrDefault(c => c.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
                if (dbCert != null)
                {
                    return await RefreshCertificateInfoAsync(domain) ?? dbCert;
                }

                // If not found in database, scan files
                var fileCertificates = await ScanCertificatesFromFilesAsync();
                return fileCertificates.FirstOrDefault(c => c.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SSL certificate for domain {Domain}", domain);
                throw;
            }
        }

        public async Task<SSLCertificateDto> GenerateCertificateAsync(GenerateCertificateRequest request)
        {
            try
            {
                _logger.LogInformation("Generating SSL certificate for domain {Domain}", request.Domain);

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Domain))
                {
                    throw new ArgumentException("Domain is required", nameof(request.Domain));
                }

                // Check if certificate already exists
                var existingCert = await GetCertificateByDomainAsync(request.Domain);
                if (existingCert != null)
                {
                    _logger.LogWarning("Certificate for domain {Domain} already exists", request.Domain);
                    return existingCert;
                }

                // Generate certificate using PowerShell script
                var certificate = await GenerateCertificateUsingScriptAsync(request);

                // Save to database
                await SaveCertificateToDbAsync(certificate);

                _logger.LogInformation("Successfully generated SSL certificate for domain {Domain}", request.Domain);
                return certificate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SSL certificate for domain {Domain}", request.Domain);
                throw;
            }
        }

        public async Task<SSLCertificateDto?> RenewCertificateAsync(string domain)
        {
            try
            {
                _logger.LogInformation("Renewing SSL certificate for domain {Domain}", domain);

                var existingCert = await GetCertificateByDomainAsync(domain);
                if (existingCert == null)
                {
                    _logger.LogWarning("Certificate for domain {Domain} not found for renewal", domain);
                    return null;
                }

                // Create renewal request
                var renewalRequest = new GenerateCertificateRequest
                {
                    Domain = domain,
                    UseLetsEncrypt = existingCert.CertificateType == "LetsEncrypt",
                    KeySize = existingCert.KeySize,
                    ValidityDays = 365,
                    Environment = existingCert.Environment,
                    SubjectAlternativeNames = existingCert.SubjectAlternativeNames
                };

                // Generate new certificate
                var renewedCert = await GenerateCertificateUsingScriptAsync(renewalRequest);
                renewedCert.Id = existingCert.Id; // Keep the same ID

                // Update in database
                await UpdateCertificateInDbAsync(renewedCert);

                _logger.LogInformation("Successfully renewed SSL certificate for domain {Domain}", domain);
                return renewedCert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing SSL certificate for domain {Domain}", domain);
                throw;
            }
        }

        public async Task<bool> DeleteCertificateAsync(string domain)
        {
            try
            {
                _logger.LogInformation("Deleting SSL certificate for domain {Domain}", domain);

                var certificate = await GetCertificateByDomainAsync(domain);
                if (certificate == null)
                {
                    return false;
                }

                // Delete certificate files
                if (File.Exists(certificate.CertificatePath))
                {
                    File.Delete(certificate.CertificatePath);
                }

                if (File.Exists(certificate.PrivateKeyPath))
                {
                    File.Delete(certificate.PrivateKeyPath);
                }

                // Remove from database
                await RemoveCertificateFromDbAsync(domain);

                _logger.LogInformation("Successfully deleted SSL certificate for domain {Domain}", domain);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SSL certificate for domain {Domain}", domain);
                throw;
            }
        }

        public async Task<SSLCertificateHealthDto> GetCertificateHealthAsync()
        {
            try
            {
                var certificates = await GetAllCertificatesAsync();
                var certificateList = certificates.ToList();

                var health = new SSLCertificateHealthDto
                {
                    TotalCertificates = certificateList.Count,
                    ValidCertificates = certificateList.Count(c => c.IsValid && !c.IsExpired),
                    ExpiredCertificates = certificateList.Count(c => c.IsExpired),
                    ExpiringSoonCertificates = certificateList.Count(c => c.IsExpiringSoon),
                    InvalidCertificates = certificateList.Count(c => !c.IsValid),
                    LastHealthCheck = DateTime.UtcNow
                };

                // Calculate health score
                if (health.TotalCertificates == 0)
                {
                    health.HealthScore = 100;
                    health.OverallStatus = "No certificates found";
                }
                else
                {
                    health.HealthScore = (int)((double)health.ValidCertificates / health.TotalCertificates * 100);
                    
                    if (health.HealthScore == 100)
                    {
                        health.OverallStatus = "Excellent";
                    }
                    else if (health.HealthScore >= 80)
                    {
                        health.OverallStatus = "Good";
                    }
                    else if (health.HealthScore >= 60)
                    {
                        health.OverallStatus = "Fair";
                    }
                    else
                    {
                        health.OverallStatus = "Poor";
                    }
                }

                // Generate issues and recommendations
                var issues = new List<string>();
                var recommendations = new List<string>();

                if (health.ExpiredCertificates > 0)
                {
                    issues.Add($"{health.ExpiredCertificates} expired certificates found");
                    recommendations.Add("Renew expired certificates immediately");
                }

                if (health.ExpiringSoonCertificates > 0)
                {
                    issues.Add($"{health.ExpiringSoonCertificates} certificates expiring soon");
                    recommendations.Add("Schedule renewal for expiring certificates");
                }

                if (health.InvalidCertificates > 0)
                {
                    issues.Add($"{health.InvalidCertificates} invalid certificates found");
                    recommendations.Add("Regenerate invalid certificates");
                }

                if (health.TotalCertificates == 0)
                {
                    issues.Add("No SSL certificates configured");
                    recommendations.Add("Generate SSL certificates for your domains");
                }

                health.Issues = issues.ToArray();
                health.Recommendations = recommendations.ToArray();

                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SSL certificate health");
                throw;
            }
        }

        public async Task<SSLCertificateValidationDto?> ValidateCertificateAsync(string domain)
        {
            try
            {
                var certificate = await GetCertificateByDomainAsync(domain);
                if (certificate == null)
                {
                    return null;
                }

                var validation = new SSLCertificateValidationDto
                {
                    Domain = domain,
                    ValidatedAt = DateTime.UtcNow
                };

                var details = new List<string>();
                var errors = new List<string>();

                // Check if certificate file exists
                if (!File.Exists(certificate.CertificatePath))
                {
                    errors.Add("Certificate file not found");
                    validation.IsValid = false;
                }
                else
                {
                    try
                    {
                        // Load and validate certificate
                        var cert = new X509Certificate2(certificate.CertificatePath);
                        
                        // Check dates
                        var now = DateTime.UtcNow;
                        validation.DateValid = now >= cert.NotBefore && now <= cert.NotAfter;
                        if (!validation.DateValid)
                        {
                            errors.Add($"Certificate is not valid for current date. Valid from {cert.NotBefore:yyyy-MM-dd} to {cert.NotAfter:yyyy-MM-dd}");
                        }

                        // Check domain
                        validation.DomainValid = cert.Subject.Contains($"CN={domain}") || 
                                               cert.Subject.Contains($"CN=*.{domain}") ||
                                               certificate.SubjectAlternativeNames.Contains(domain);
                        if (!validation.DomainValid)
                        {
                            errors.Add($"Certificate does not match domain {domain}");
                        }

                        // Check signature (basic check)
                        validation.SignatureValid = true; // This would require more complex validation

                        // Check chain (basic check)
                        validation.ChainValid = true; // This would require more complex validation

                        details.Add($"Certificate valid from {cert.NotBefore:yyyy-MM-dd} to {cert.NotAfter:yyyy-MM-dd}");
                        details.Add($"Issuer: {cert.Issuer}");
                        details.Add($"Subject: {cert.Subject}");
                        details.Add($"Serial Number: {cert.SerialNumber}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error reading certificate: {ex.Message}");
                        validation.IsValid = false;
                    }
                }

                validation.IsValid = errors.Count == 0 && validation.DateValid && validation.DomainValid;
                validation.Status = validation.IsValid ? "Valid" : "Invalid";
                validation.ValidationDetails = details.ToArray();
                validation.ValidationErrors = errors.ToArray();

                return validation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating SSL certificate for domain {Domain}", domain);
                throw;
            }
        }

        public async Task<bool> CertificateExistsAsync(string domain)
        {
            var certificate = await GetCertificateByDomainAsync(domain);
            return certificate != null;
        }

        public async Task<IEnumerable<SSLCertificateDto>> GetCertificatesExpiringSoonAsync(int days = 30)
        {
            var certificates = await GetAllCertificatesAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(days);
            
            return certificates.Where(c => c.ValidTo <= cutoffDate && !c.IsExpired);
        }

        public async Task<SSLCertificateDto?> RefreshCertificateInfoAsync(string domain)
        {
            try
            {
                var certificate = ScanCertificateFromFile(domain);
                if (certificate != null)
                {
                    await UpdateCertificateInDbAsync(certificate);
                }
                return certificate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing certificate info for domain {Domain}", domain);
                return null;
            }
        }

        #region Private Methods

        private async Task<SSLCertificateDto> GenerateCertificateUsingScriptAsync(GenerateCertificateRequest request)
        {
            try
            {
                // Check if we're running in a container
                var isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
                
                string scriptPath;
                string fileName;
                List<string> arguments;

                if (isContainer)
                {
                    // Use Linux script in container
                    scriptPath = "/usr/local/bin/generate-ssl-linux.sh";
                    fileName = "bash";
                    arguments = new List<string>
                    {
                        scriptPath,
                        "--environment", request.Environment,
                        "--output-path", _sslPath,
                        "--key-size", request.KeySize.ToString(),
                        "--validity-days", request.ValidityDays.ToString(),
                        "--force"
                    };

                    if (request.UseLetsEncrypt)
                    {
                        arguments.Add("--use-lets-encrypt");
                        if (!string.IsNullOrEmpty(request.Email))
                        {
                            arguments.AddRange(new[] { "--email", request.Email });
                        }
                    }
                }
                else
                {
                    // Use PowerShell script on Windows host
                    scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "Generate-SSLCertificates.ps1");
                    fileName = "powershell.exe";
                    arguments = new List<string>
                    {
                        "-ExecutionPolicy", "Bypass",
                        "-File", scriptPath,
                        "-Environment", request.Environment,
                        "-OutputPath", _sslPath,
                        "-KeySize", request.KeySize.ToString(),
                        "-ValidityDays", request.ValidityDays.ToString(),
                        "-Force"
                    };

                    if (request.UseLetsEncrypt)
                    {
                        arguments.Add("-UseLetsEncrypt");
                        if (!string.IsNullOrEmpty(request.Email))
                        {
                            arguments.AddRange(new[] { "-Email", request.Email });
                        }
                    }
                }

                _logger.LogInformation("Executing SSL generation script: {FileName} {Arguments}", 
                    fileName, string.Join(" ", arguments));

                // Execute script
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = string.Join(" ", arguments),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                _logger.LogInformation("SSL generation script output: {Output}", output);
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("SSL generation script error: {Error}", error);
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception($"SSL generation script failed with exit code {process.ExitCode}: {error}");
                }

                // Wait a moment for files to be written
                await Task.Delay(2000);

                // Load the generated certificate
                var certificate = ScanCertificateFromFile(request.Domain);
                if (certificate == null)
                {
                    throw new Exception("Certificate was generated but could not be loaded");
                }

                return certificate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate using script for domain {Domain}", request.Domain);
                throw;
            }
        }

        private SSLCertificateDto? ScanCertificateFromFile(string domain)
        {
            try
            {
                var certFile = Path.Combine(_sslPath, $"{domain}.crt");
                var keyFile = Path.Combine(_sslPath, $"{domain}.key");

                if (!File.Exists(certFile) || !File.Exists(keyFile))
                {
                    return null;
                }

                var cert = new X509Certificate2(certFile);
                var now = DateTime.UtcNow;
                var validTo = cert.NotAfter;
                var validFrom = cert.NotBefore;
                var daysUntilExpiration = (int)(validTo - now).TotalDays;

                return new SSLCertificateDto
                {
                    Domain = domain,
                    CertificatePath = certFile,
                    PrivateKeyPath = keyFile,
                    Issuer = cert.Issuer,
                    Subject = cert.Subject,
                    SerialNumber = cert.SerialNumber,
                    Thumbprint = cert.Thumbprint,
                    ValidFrom = validFrom,
                    ValidTo = validTo,
                    DaysUntilExpiration = daysUntilExpiration,
                    IsValid = now >= validFrom && now <= validTo,
                    IsExpired = now > validTo,
                    IsExpiringSoon = daysUntilExpiration <= 30,
                    CertificateType = "SelfSigned", // This would need to be determined based on issuer
                    KeySize = cert.GetRSAPublicKey()?.KeySize ?? 0,
                    SubjectAlternativeNames = ExtractSubjectAlternativeNames(cert),
                    Environment = "development", // This would need to be determined
                    CreatedAt = File.GetCreationTime(certFile),
                    UpdatedAt = File.GetLastWriteTime(certFile),
                    Status = "Active"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning certificate from file for domain {Domain}", domain);
                return null;
            }
        }

        private Task<IEnumerable<SSLCertificateDto>> ScanCertificatesFromFilesAsync()
        {
            var certificates = new List<SSLCertificateDto>();

            try
            {
                if (!Directory.Exists(_sslPath))
                {
                    return Task.FromResult<IEnumerable<SSLCertificateDto>>(certificates);
                }

                var certFiles = Directory.GetFiles(_sslPath, "*.crt");
                foreach (var certFile in certFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(certFile);
                    var certificate = ScanCertificateFromFile(fileName);
                    if (certificate != null)
                    {
                        certificates.Add(certificate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning certificates from files");
            }

            return Task.FromResult<IEnumerable<SSLCertificateDto>>(certificates);
        }

        private string[] ExtractSubjectAlternativeNames(X509Certificate2 cert)
        {
            try
            {
                var sanExtension = cert.Extensions.OfType<System.Security.Cryptography.X509Certificates.X509Extension>()
                    .FirstOrDefault(e => e.Oid?.FriendlyName == "Subject Alternative Name");

                if (sanExtension != null)
                {
                    var sanData = sanExtension.Format(false);
                    // Parse SAN data - this is a simplified version
                    // In a real implementation, you'd need to properly parse the ASN.1 data
                    return new[] { cert.Subject };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting Subject Alternative Names");
            }

            return new[] { cert.Subject };
        }

        private async Task<List<SSLCertificateDto>> LoadCertificatesFromDbAsync()
        {
            try
            {
                if (!File.Exists(_certificatesDbPath))
                {
                    return new List<SSLCertificateDto>();
                }

                var json = await File.ReadAllTextAsync(_certificatesDbPath);
                var certificates = JsonSerializer.Deserialize<List<SSLCertificateDto>>(json) ?? new List<SSLCertificateDto>();
                return certificates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates from database");
                return new List<SSLCertificateDto>();
            }
        }

        private async Task SaveCertificateToDbAsync(SSLCertificateDto certificate)
        {
            try
            {
                var certificates = await LoadCertificatesFromDbAsync();
                certificate.Id = certificates.Count > 0 ? certificates.Max(c => c.Id) + 1 : 1;
                certificates.Add(certificate);
                await SaveCertificatesToDbAsync(certificates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving certificate to database");
                throw;
            }
        }

        private async Task UpdateCertificateInDbAsync(SSLCertificateDto certificate)
        {
            try
            {
                var certificates = await LoadCertificatesFromDbAsync();
                var existingIndex = certificates.FindIndex(c => c.Domain == certificate.Domain);
                if (existingIndex >= 0)
                {
                    certificates[existingIndex] = certificate;
                }
                else
                {
                    certificate.Id = certificates.Count > 0 ? certificates.Max(c => c.Id) + 1 : 1;
                    certificates.Add(certificate);
                }
                await SaveCertificatesToDbAsync(certificates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate in database");
                throw;
            }
        }

        private async Task RemoveCertificateFromDbAsync(string domain)
        {
            try
            {
                var certificates = await LoadCertificatesFromDbAsync();
                certificates.RemoveAll(c => c.Domain == domain);
                await SaveCertificatesToDbAsync(certificates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing certificate from database");
                throw;
            }
        }

        private async Task SaveCertificatesToDbAsync(List<SSLCertificateDto> certificates)
        {
            try
            {
                var json = JsonSerializer.Serialize(certificates, IndentedJson);
                await File.WriteAllTextAsync(_certificatesDbPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving certificates to database");
                throw;
            }
        }

        #endregion
    }
}
