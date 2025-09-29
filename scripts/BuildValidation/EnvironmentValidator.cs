using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Environment validation and health checks system implementing design document requirements
    /// Validates configuration, connectivity, and certificates per escalation matrix
    /// </summary>
    public class EnvironmentValidator
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;
        private readonly Dictionary<string, IEnvironmentValidator> _validators;

        public EnvironmentValidator(string workspaceRoot, ILogger logger)
        {
            _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validators = InitializeValidators();
        }

        /// <summary>
        /// Performs comprehensive environment validation per design document
        /// </summary>
        public async Task<EnvironmentValidationResult> ValidateEnvironmentAsync()
        {
            var result = new EnvironmentValidationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo("Starting environment validation");

                // Database validation
                var dbResult = await _validators["Database"].ValidateAsync();
                result.AddValidationResult("Database", dbResult);

                // Authentication validation
                var authResult = await _validators["Authentication"].ValidateAsync();
                result.AddValidationResult("Authentication", authResult);

                // Networking validation
                var networkResult = await _validators["Networking"].ValidateAsync();
                result.AddValidationResult("Networking", networkResult);

                // SSL/TLS validation
                var sslResult = await _validators["SSL"].ValidateAsync();
                result.AddValidationResult("SSL", sslResult);

                // Configuration files validation
                var configResult = await ValidateConfigurationFilesAsync();
                result.AddValidationResult("Configuration", configResult);

                // Health endpoints validation
                var healthResult = await ValidateHealthEndpointsAsync();
                result.AddValidationResult("HealthChecks", healthResult);

                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = DetermineOverallStatus(result);

                _logger.LogInfo($"Environment validation completed in {result.Duration.TotalSeconds:F2}s with status: {result.OverallStatus}");
            }
            catch (Exception ex)
            {
                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = ValidationStatus.Error;
                result.AddError(new EnvironmentValidationError
                {
                    Category = "System",
                    Message = $"Environment validation failed: {ex.Message}",
                    Severity = ErrorSeverity.Critical,
                    Exception = ex
                });

                _logger.LogError($"Environment validation failed: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Validates health endpoints for service availability monitoring
        /// </summary>
        public async Task<HealthCheckResult> ValidateHealthEndpointsAsync()
        {
            var result = new HealthCheckResult();

            try
            {
                _logger.LogInfo("Validating health endpoints");

                var healthEndpoints = new Dictionary<string, string>
                {
                    ["API"] = GetApiHealthEndpoint(),
                    ["Web"] = GetWebHealthEndpoint(),
                    ["Database"] = GetDatabaseConnectionString()
                };

                foreach (var endpoint in healthEndpoints)
                {
                    var healthStatus = await CheckEndpointHealthAsync(endpoint.Key, endpoint.Value);
                    result.EndpointResults[endpoint.Key] = healthStatus;

                    if (!healthStatus.IsHealthy)
                    {
                        result.UnhealthyEndpoints.Add(endpoint.Key);
                        result.Issues.Add($"{endpoint.Key} endpoint is unhealthy: {healthStatus.ErrorMessage}");
                    }
                }

                result.OverallHealthy = result.UnhealthyEndpoints.Count == 0;
                _logger.LogInfo($"Health check completed. Healthy endpoints: {result.EndpointResults.Count - result.UnhealthyEndpoints.Count}/{result.EndpointResults.Count}");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.OverallHealthy = false;
                _logger.LogError($"Health endpoint validation failed: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Validates configuration files for all environments
        /// </summary>
        private async Task<ComponentValidationResult> ValidateConfigurationFilesAsync()
        {
            var result = new ComponentValidationResult("Configuration");

            try
            {
                var configFiles = new[]
                {
                    Path.Combine(_workspaceRoot, "src/Inventory.API/appsettings.json"),
                    Path.Combine(_workspaceRoot, "src/Inventory.API/appsettings.Development.json"),
                    Path.Combine(_workspaceRoot, "src/Inventory.API/appsettings.Production.json"),
                    Path.Combine(_workspaceRoot, "src/Inventory.Web.Client/appsettings.json"),
                    Path.Combine(_workspaceRoot, "src/Inventory.Web.Client/appsettings.Production.json")
                };

                foreach (var configFile in configFiles)
                {
                    var configValidation = await ValidateConfigurationFileAsync(configFile);
                    result.SubResults[Path.GetFileName(configFile)] = configValidation;

                    if (!configValidation.IsValid)
                    {
                        foreach (var issue in configValidation.Issues)
                        {
                            result.Issues.Add($"{Path.GetFileName(configFile)}: {issue}");
                        }
                    }
                }

                result.IsValid = result.Issues.Count == 0;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"Configuration validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private async Task<ConfigurationFileValidationResult> ValidateConfigurationFileAsync(string configPath)
        {
            var result = new ConfigurationFileValidationResult(configPath);

            try
            {
                if (!File.Exists(configPath))
                {
                    result.Issues.Add("Configuration file not found");
                    result.IsValid = false;
                    return result;
                }

                var content = await File.ReadAllTextAsync(configPath);
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    result.Issues.Add("Configuration file is empty");
                    result.IsValid = false;
                    return result;
                }

                // Validate JSON structure
                try
                {
                    JsonDocument.Parse(content);
                }
                catch (JsonException ex)
                {
                    result.Issues.Add($"Invalid JSON format: {ex.Message}");
                    result.IsValid = false;
                    return result;
                }

                // Validate required sections based on file type
                if (configPath.Contains("API"))
                {
                    ValidateApiConfiguration(content, result);
                }
                else if (configPath.Contains("Web.Client"))
                {
                    ValidateWebClientConfiguration(content, result);
                }

                result.IsValid = result.Issues.Count == 0;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"Configuration file validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private void ValidateApiConfiguration(string content, ConfigurationFileValidationResult result)
        {
            var requiredSections = new[]
            {
                "ConnectionStrings",
                "Jwt",
                "Logging"
            };

            foreach (var section in requiredSections)
            {
                if (!content.Contains($"\"{section}\""))
                {
                    result.Issues.Add($"Missing required section: {section}");
                }
            }

            // Validate JWT configuration structure
            if (content.Contains("\"Jwt\""))
            {
                var jwtRequiredFields = new[] { "Key", "Issuer", "Audience" };
                foreach (var field in jwtRequiredFields)
                {
                    if (!content.Contains($"\"{field}\""))
                    {
                        result.Issues.Add($"Missing JWT field: {field}");
                    }
                }
            }
        }

        private void ValidateWebClientConfiguration(string content, ConfigurationFileValidationResult result)
        {
            var requiredSections = new[]
            {
                "ApiUrl",
                "Logging"
            };

            foreach (var section in requiredSections)
            {
                if (!content.Contains($"\"{section}\""))
                {
                    result.Issues.Add($"Missing required section: {section}");
                }
            }
        }

        private async Task<EndpointHealthStatus> CheckEndpointHealthAsync(string name, string endpoint)
        {
            var status = new EndpointHealthStatus(name, endpoint);

            try
            {
                if (name == "Database")
                {
                    // Database connection check
                    status.IsHealthy = await CheckDatabaseConnectivityAsync(endpoint);
                    if (!status.IsHealthy)
                        status.ErrorMessage = "Database connection failed";
                }
                else
                {
                    // HTTP endpoint check
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var response = await client.GetAsync(endpoint);
                    status.IsHealthy = response.IsSuccessStatusCode;
                    status.ResponseTime = TimeSpan.FromMilliseconds(100); // Simplified
                    
                    if (!status.IsHealthy)
                        status.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.ErrorMessage = ex.Message;
                status.Exception = ex;
            }

            return status;
        }

        private async Task<bool> CheckDatabaseConnectivityAsync(string connectionString)
        {
            try
            {
                // Simplified database connectivity check
                // In real implementation, use Npgsql to test PostgreSQL connection
                var host = ExtractHostFromConnectionString(connectionString);
                var port = ExtractPortFromConnectionString(connectionString);

                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port);
                return tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }

        private Dictionary<string, IEnvironmentValidator> InitializeValidators()
        {
            return new Dictionary<string, IEnvironmentValidator>
            {
                ["Database"] = new DatabaseValidator(_logger),
                ["Authentication"] = new AuthenticationValidator(_logger),
                ["Networking"] = new NetworkingValidator(_logger),
                ["SSL"] = new SslValidator(_logger)
            };
        }

        private ValidationStatus DetermineOverallStatus(EnvironmentValidationResult result)
        {
            if (result.ValidationResults.Any(v => v.Value.OverallStatus == ValidationStatus.Error))
                return ValidationStatus.Error;
            
            if (result.ValidationResults.Any(v => v.Value.OverallStatus == ValidationStatus.Failed))
                return ValidationStatus.Failed;
            
            return ValidationStatus.Passed;
        }

        private string GetApiHealthEndpoint()
        {
            var serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "localhost";
            var apiPort = Environment.GetEnvironmentVariable("API_PORT") ?? "5000";
            return $"http://{serverIp}:{apiPort}/health";
        }

        private string GetWebHealthEndpoint()
        {
            var serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "localhost";
            var webPort = Environment.GetEnvironmentVariable("WEB_PORT") ?? "5001";
            return $"http://{serverIp}:{webPort}/health";
        }

        private string GetDatabaseConnectionString()
        {
            return Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                ?? "Host=localhost;Database=inventoryctrl;Username=postgres;Password=postgres";
        }

        private string ExtractHostFromConnectionString(string connectionString)
        {
            var hostMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Host=([^;]+)");
            return hostMatch.Success ? hostMatch.Groups[1].Value : "localhost";
        }

        private int ExtractPortFromConnectionString(string connectionString)
        {
            var portMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Port=([^;]+)");
            return portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var port) ? port : 5432;
        }
    }

    #region Specific Validators

    public interface IEnvironmentValidator
    {
        Task<ComponentValidationResult> ValidateAsync();
    }

    public class DatabaseValidator : IEnvironmentValidator
    {
        private readonly ILogger _logger;

        public DatabaseValidator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ComponentValidationResult> ValidateAsync()
        {
            var result = new ComponentValidationResult("Database");

            try
            {
                var requiredVars = new[] { "ConnectionStrings__DefaultConnection", "POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD" };
                
                foreach (var variable in requiredVars)
                {
                    var value = Environment.GetEnvironmentVariable(variable);
                    if (string.IsNullOrEmpty(value))
                    {
                        result.Issues.Add($"Required environment variable not set: {variable}");
                    }
                    else
                    {
                        result.ValidatedItems.Add($"{variable}: ✓");
                    }
                }

                // Test database connectivity
                var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var connectivityResult = await TestDatabaseConnectivityAsync(connectionString);
                    if (!connectivityResult.Success)
                    {
                        result.Issues.Add($"Database connectivity failed: {connectivityResult.ErrorMessage}");
                    }
                    else
                    {
                        result.ValidatedItems.Add("Database connectivity: ✓");
                    }
                }

                result.IsValid = result.Issues.Count == 0;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"Database validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private async Task<ConnectivityTestResult> TestDatabaseConnectivityAsync(string connectionString)
        {
            var result = new ConnectivityTestResult();

            try
            {
                // Simplified connectivity test
                var host = ExtractHostFromConnectionString(connectionString);
                var port = ExtractPortFromConnectionString(connectionString);

                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port);
                
                result.Success = tcpClient.Connected;
                if (!result.Success)
                    result.ErrorMessage = "TCP connection failed";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private string ExtractHostFromConnectionString(string connectionString)
        {
            var hostMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Host=([^;]+)");
            return hostMatch.Success ? hostMatch.Groups[1].Value : "localhost";
        }

        private int ExtractPortFromConnectionString(string connectionString)
        {
            var portMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Port=([^;]+)");
            return portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var port) ? port : 5432;
        }
    }

    public class AuthenticationValidator : IEnvironmentValidator
    {
        private readonly ILogger _logger;

        public AuthenticationValidator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ComponentValidationResult> ValidateAsync()
        {
            var result = new ComponentValidationResult("Authentication");

            try
            {
                var requiredVars = new[] { "Jwt__Key", "Jwt__Issuer", "Jwt__Audience" };
                
                foreach (var variable in requiredVars)
                {
                    var value = Environment.GetEnvironmentVariable(variable);
                    if (string.IsNullOrEmpty(value))
                    {
                        result.Issues.Add($"Required JWT variable not set: {variable}");
                    }
                    else
                    {
                        // Validate JWT key strength
                        if (variable == "Jwt__Key" && value.Length < 32)
                        {
                            result.Issues.Add("JWT key should be at least 32 characters long");
                        }
                        else
                        {
                            result.ValidatedItems.Add($"{variable}: ✓");
                        }
                    }
                }

                result.IsValid = result.Issues.Count == 0;
                await Task.CompletedTask; // Placeholder for async operations
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"Authentication validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }
    }

    public class NetworkingValidator : IEnvironmentValidator
    {
        private readonly ILogger _logger;

        public NetworkingValidator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ComponentValidationResult> ValidateAsync()
        {
            var result = new ComponentValidationResult("Networking");

            try
            {
                var requiredVars = new[] { "SERVER_IP", "DOMAIN", "ASPNETCORE_URLS" };
                
                foreach (var variable in requiredVars)
                {
                    var value = Environment.GetEnvironmentVariable(variable);
                    if (string.IsNullOrEmpty(value))
                    {
                        result.Issues.Add($"Required network variable not set: {variable}");
                    }
                    else
                    {
                        result.ValidatedItems.Add($"{variable}: {value}");
                    }
                }

                // Test network connectivity
                var connectivityTests = await TestNetworkConnectivityAsync();
                foreach (var test in connectivityTests)
                {
                    if (test.Success)
                    {
                        result.ValidatedItems.Add($"Connectivity to {test.Target}: ✓");
                    }
                    else
                    {
                        result.Issues.Add($"Connectivity to {test.Target} failed: {test.ErrorMessage}");
                    }
                }

                result.IsValid = result.Issues.Count == 0;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"Networking validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private async Task<List<ConnectivityTestResult>> TestNetworkConnectivityAsync()
        {
            var results = new List<ConnectivityTestResult>();
            var targets = new[] { "google.com", "github.com", "nuget.org" };

            foreach (var target in targets)
            {
                var result = new ConnectivityTestResult { Target = target };

                try
                {
                    var ping = new Ping();
                    var reply = await ping.SendPingAsync(target, 5000);
                    result.Success = reply.Status == IPStatus.Success;
                    if (!result.Success)
                        result.ErrorMessage = reply.Status.ToString();
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                }

                results.Add(result);
            }

            return results;
        }
    }

    public class SslValidator : IEnvironmentValidator
    {
        private readonly ILogger _logger;

        public SslValidator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ComponentValidationResult> ValidateAsync()
        {
            var result = new ComponentValidationResult("SSL");

            try
            {
                var certPath = Environment.GetEnvironmentVariable("SSL_CERT_PATH");
                var keyPath = Environment.GetEnvironmentVariable("SSL_KEY_PATH");

                if (string.IsNullOrEmpty(certPath))
                {
                    result.Issues.Add("SSL_CERT_PATH environment variable not set");
                }
                else if (!File.Exists(certPath))
                {
                    result.Issues.Add($"SSL certificate file not found: {certPath}");
                }
                else
                {
                    var certValidation = await ValidateCertificateAsync(certPath);
                    if (certValidation.IsValid)
                    {
                        result.ValidatedItems.Add($"SSL certificate: ✓ (expires: {certValidation.ExpirationDate:yyyy-MM-dd})");
                    }
                    else
                    {
                        result.Issues.Add($"SSL certificate validation failed: {certValidation.ErrorMessage}");
                    }
                }

                if (string.IsNullOrEmpty(keyPath))
                {
                    result.Issues.Add("SSL_KEY_PATH environment variable not set");
                }
                else if (!File.Exists(keyPath))
                {
                    result.Issues.Add($"SSL key file not found: {keyPath}");
                }
                else
                {
                    result.ValidatedItems.Add("SSL key file: ✓");
                }

                result.IsValid = result.Issues.Count == 0;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"SSL validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private async Task<CertificateValidationResult> ValidateCertificateAsync(string certPath)
        {
            var result = new CertificateValidationResult();

            try
            {
                var certificate = new X509Certificate2(certPath);
                result.IsValid = certificate.NotAfter > DateTime.Now.AddDays(30); // At least 30 days validity
                result.ExpirationDate = certificate.NotAfter;
                
                if (!result.IsValid)
                    result.ErrorMessage = "Certificate expires within 30 days";

                await Task.CompletedTask; // Placeholder for async operations
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }

    #endregion

    #region Result Classes

    public class EnvironmentValidationResult
    {
        public ValidationStatus OverallStatus { get; set; }
        public Dictionary<string, ComponentValidationResult> ValidationResults { get; } = new();
        public List<EnvironmentValidationError> Errors { get; } = new();
        public TimeSpan Duration { get; set; }

        public void AddValidationResult(string component, ComponentValidationResult result)
        {
            ValidationResults[component] = result;
            
            if (!result.IsValid)
            {
                foreach (var issue in result.Issues)
                {
                    Errors.Add(new EnvironmentValidationError
                    {
                        Category = component,
                        Message = issue,
                        Severity = ErrorSeverity.High
                    });
                }
            }
        }

        public void AddError(EnvironmentValidationError error)
        {
            Errors.Add(error);
        }
    }

    public class ComponentValidationResult
    {
        public string Component { get; }
        public bool IsValid { get; set; } = true;
        public List<string> Issues { get; } = new();
        public List<string> ValidatedItems { get; } = new();
        public Dictionary<string, object> SubResults { get; } = new();
        public ValidationStatus OverallStatus { get; set; } = ValidationStatus.Passed;
        public Exception? Exception { get; set; }

        public ComponentValidationResult(string component)
        {
            Component = component;
        }
    }

    public class HealthCheckResult
    {
        public bool OverallHealthy { get; set; }
        public Dictionary<string, EndpointHealthStatus> EndpointResults { get; } = new();
        public List<string> UnhealthyEndpoints { get; } = new();
        public List<string> Issues { get; } = new();
        public Exception? Exception { get; set; }
    }

    public class EndpointHealthStatus
    {
        public string Name { get; }
        public string Endpoint { get; }
        public bool IsHealthy { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public Exception? Exception { get; set; }

        public EndpointHealthStatus(string name, string endpoint)
        {
            Name = name;
            Endpoint = endpoint;
        }
    }

    public class ConfigurationFileValidationResult
    {
        public string FilePath { get; }
        public bool IsValid { get; set; } = true;
        public List<string> Issues { get; } = new();
        public Exception? Exception { get; set; }

        public ConfigurationFileValidationResult(string filePath)
        {
            FilePath = filePath;
        }
    }

    public class ConnectivityTestResult
    {
        public string Target { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CertificateValidationResult
    {
        public bool IsValid { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class EnvironmentValidationError
    {
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ErrorSeverity Severity { get; set; }
        public Exception? Exception { get; set; }
    }

    #endregion
}