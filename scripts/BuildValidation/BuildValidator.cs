using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Comprehensive build validation system for InventoryCtrl_2
    /// Implements error detection, classification, and resolution strategies from design document
    /// </summary>
    public class BuildValidator
    {
        private readonly string _workspaceRoot;
        private readonly IErrorClassifier _errorClassifier;
        private readonly ILogger _logger;
        private readonly BuildConfiguration _config;

        public BuildValidator(string workspaceRoot, IErrorClassifier errorClassifier, ILogger logger)
        {
            _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
            _errorClassifier = errorClassifier ?? throw new ArgumentNullException(nameof(errorClassifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = LoadBuildConfiguration();
        }

        /// <summary>
        /// Performs comprehensive build validation across all project components
        /// </summary>
        public async Task<BuildValidationResult> ValidateAsync()
        {
            var result = new BuildValidationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo("Starting comprehensive build validation");

                // Phase 1: Dependency Analysis
                var dependencyResult = await ValidateDependenciesAsync();
                result.AddPhaseResult("Dependencies", dependencyResult);

                // Phase 2: Project Reference Validation
                var referenceResult = await ValidateProjectReferencesAsync();
                result.AddPhaseResult("ProjectReferences", referenceResult);

                // Phase 3: Compilation Validation
                var compilationResult = await ValidateCompilationAsync();
                result.AddPhaseResult("Compilation", compilationResult);

                // Phase 4: Docker Build Validation
                var dockerResult = await ValidateDockerBuildsAsync();
                result.AddPhaseResult("Docker", dockerResult);

                // Phase 5: Environment Configuration Validation
                var envResult = await ValidateEnvironmentConfigurationAsync();
                result.AddPhaseResult("Environment", envResult);

                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = DetermineOverallStatus(result);

                _logger.LogInfo($"Build validation completed in {result.Duration.TotalSeconds:F2}s with status: {result.OverallStatus}");
            }
            catch (Exception ex)
            {
                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = ValidationStatus.Error;
                result.AddError(new BuildError
                {
                    Category = ErrorCategory.SystemError,
                    Message = $"Build validation failed with exception: {ex.Message}",
                    Source = "BuildValidator",
                    Severity = ErrorSeverity.Critical,
                    Exception = ex
                });

                _logger.LogError($"Build validation failed: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Validates package dependencies across all projects
        /// Implements dependency resolution strategy from design document
        /// </summary>
        private async Task<PhaseResult> ValidateDependenciesAsync()
        {
            var result = new PhaseResult("Dependencies");
            
            try
            {
                // Load Directory.Packages.props for centralized package management
                var packagesPropsPath = Path.Combine(_workspaceRoot, "Directory.Packages.props");
                if (!File.Exists(packagesPropsPath))
                {
                    result.AddError(new BuildError
                    {
                        Category = ErrorCategory.PackageReference,
                        Message = "Directory.Packages.props not found - centralized package management required",
                        Source = packagesPropsPath,
                        Severity = ErrorSeverity.High
                    });
                    return result;
                }

                // Validate package versions consistency
                var packageVersions = await ParsePackageVersionsAsync(packagesPropsPath);
                var projectFiles = GetAllProjectFiles();

                foreach (var projectFile in projectFiles)
                {
                    var projectDependencies = await ParseProjectDependenciesAsync(projectFile);
                    ValidateProjectPackageReferences(projectFile, projectDependencies, packageVersions, result);
                }

                // Check for common compatibility issues
                ValidateFrameworkCompatibility(packageVersions, result);
                await ValidatePackageRestoreAsync(result);

                result.Status = result.Errors.Any(e => e.Severity >= ErrorSeverity.High) 
                    ? ValidationStatus.Failed 
                    : ValidationStatus.Passed;
            }
            catch (Exception ex)
            {
                result.AddError(new BuildError
                {
                    Category = ErrorCategory.SystemError,
                    Message = $"Dependency validation failed: {ex.Message}",
                    Source = "DependencyValidator",
                    Severity = ErrorSeverity.Critical,
                    Exception = ex
                });
                result.Status = ValidationStatus.Error;
            }

            return result;
        }

        /// <summary>
        /// Validates project references and build order dependencies
        /// </summary>
        private async Task<PhaseResult> ValidateProjectReferencesAsync()
        {
            var result = new PhaseResult("ProjectReferences");

            try
            {
                var projectFiles = GetAllProjectFiles();
                var dependencyGraph = await BuildDependencyGraphAsync(projectFiles);

                // Check for circular dependencies
                var circularDependencies = DetectCircularDependencies(dependencyGraph);
                if (circularDependencies.Any())
                {
                    foreach (var cycle in circularDependencies)
                    {
                        result.AddError(new BuildError
                        {
                            Category = ErrorCategory.ProjectReference,
                            Message = $"Circular dependency detected: {string.Join(" -> ", cycle)}",
                            Source = "ProjectReferenceValidator",
                            Severity = ErrorSeverity.Critical
                        });
                    }
                }

                // Validate build order according to design document
                ValidateBuildOrder(dependencyGraph, result);

                result.Status = result.Errors.Any(e => e.Severity >= ErrorSeverity.High) 
                    ? ValidationStatus.Failed 
                    : ValidationStatus.Passed;
            }
            catch (Exception ex)
            {
                result.AddError(new BuildError
                {
                    Category = ErrorCategory.SystemError,
                    Message = $"Project reference validation failed: {ex.Message}",
                    Source = "ProjectReferenceValidator",
                    Severity = ErrorSeverity.Critical,
                    Exception = ex
                });
                result.Status = ValidationStatus.Error;
            }

            return result;
        }

        /// <summary>
        /// Validates compilation across all projects in proper build order
        /// </summary>
        private async Task<PhaseResult> ValidateCompilationAsync()
        {
            var result = new PhaseResult("Compilation");

            try
            {
                var buildOrder = GetOptimalBuildOrder();
                
                foreach (var projectPath in buildOrder)
                {
                    var compilationResult = await CompileProjectAsync(projectPath);
                    if (!compilationResult.Success)
                    {
                        foreach (var error in compilationResult.Errors)
                        {
                            var classifiedError = _errorClassifier.ClassifyCompilationError(error);
                            result.AddError(classifiedError);
                        }
                    }
                }

                result.Status = result.Errors.Any(e => e.Severity >= ErrorSeverity.High) 
                    ? ValidationStatus.Failed 
                    : ValidationStatus.Passed;
            }
            catch (Exception ex)
            {
                result.AddError(new BuildError
                {
                    Category = ErrorCategory.SystemError,
                    Message = $"Compilation validation failed: {ex.Message}",
                    Source = "CompilationValidator",
                    Severity = ErrorSeverity.Critical,
                    Exception = ex
                });
                result.Status = ValidationStatus.Error;
            }

            return result;
        }

        /// <summary>
        /// Validates Docker build processes for API and Web Client
        /// </summary>
        private async Task<PhaseResult> ValidateDockerBuildsAsync()
        {
            var result = new PhaseResult("Docker");

            try
            {
                var dockerfiles = new[]
                {
                    Path.Combine(_workspaceRoot, "src/Inventory.API/Dockerfile"),
                    Path.Combine(_workspaceRoot, "src/Inventory.Web.Client/Dockerfile"),
                    Path.Combine(_workspaceRoot, "deploy/nginx/Dockerfile")
                };

                foreach (var dockerfile in dockerfiles)
                {
                    if (File.Exists(dockerfile))
                    {
                        await ValidateDockerfileAsync(dockerfile, result);
                    }
                    else
                    {
                        result.AddError(new BuildError
                        {
                            Category = ErrorCategory.DockerBuild,
                            Message = $"Dockerfile not found: {dockerfile}",
                            Source = dockerfile,
                            Severity = ErrorSeverity.High
                        });
                    }
                }

                // Validate docker-compose configurations
                await ValidateDockerComposeAsync(result);

                result.Status = result.Errors.Any(e => e.Severity >= ErrorSeverity.High) 
                    ? ValidationStatus.Failed 
                    : ValidationStatus.Passed;
            }
            catch (Exception ex)
            {
                result.AddError(new BuildError
                {
                    Category = ErrorCategory.SystemError,
                    Message = $"Docker validation failed: {ex.Message}",
                    Source = "DockerValidator",
                    Severity = ErrorSeverity.Critical,
                    Exception = ex
                });
                result.Status = ValidationStatus.Error;
            }

            return result;
        }

        /// <summary>
        /// Validates environment configuration and required variables
        /// </summary>
        private async Task<PhaseResult> ValidateEnvironmentConfigurationAsync()
        {
            var result = new PhaseResult("Environment");

            try
            {
                // Validate required environment variables per design document
                var requiredVars = new Dictionary<string, string[]>
                {
                    ["Database"] = new[] { "ConnectionStrings__DefaultConnection", "POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD" },
                    ["Authentication"] = new[] { "Jwt__Key", "Jwt__Issuer", "Jwt__Audience" },
                    ["Networking"] = new[] { "SERVER_IP", "DOMAIN", "ASPNETCORE_URLS" },
                    ["SSL"] = new[] { "SSL_CERT_PATH", "SSL_KEY_PATH" }
                };

                foreach (var category in requiredVars)
                {
                    foreach (var variable in category.Value)
                    {
                        var value = Environment.GetEnvironmentVariable(variable);
                        if (string.IsNullOrEmpty(value))
                        {
                            result.AddError(new BuildError
                            {
                                Category = ErrorCategory.EnvironmentConfiguration,
                                Message = $"Required environment variable not set: {variable} ({category.Key})",
                                Source = "EnvironmentValidator",
                                Severity = ErrorSeverity.High
                            });
                        }
                    }
                }

                // Validate configuration files
                await ValidateConfigurationFilesAsync(result);

                result.Status = result.Errors.Any(e => e.Severity >= ErrorSeverity.High) 
                    ? ValidationStatus.Failed 
                    : ValidationStatus.Passed;
            }
            catch (Exception ex)
            {
                result.AddError(new BuildError
                {
                    Category = ErrorCategory.SystemError,
                    Message = $"Environment validation failed: {ex.Message}",
                    Source = "EnvironmentValidator",
                    Severity = ErrorSeverity.Critical,
                    Exception = ex
                });
                result.Status = ValidationStatus.Error;
            }

            return result;
        }

        #region Helper Methods

        private BuildConfiguration LoadBuildConfiguration()
        {
            var configPath = Path.Combine(_workspaceRoot, "scripts/BuildValidation/build-config.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<BuildConfiguration>(json) ?? new BuildConfiguration();
            }
            return new BuildConfiguration();
        }

        private List<string> GetAllProjectFiles()
        {
            return Directory.GetFiles(_workspaceRoot, "*.csproj", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();
        }

        private ValidationStatus DetermineOverallStatus(BuildValidationResult result)
        {
            if (result.PhaseResults.Any(p => p.Status == ValidationStatus.Error))
                return ValidationStatus.Error;
            
            if (result.PhaseResults.Any(p => p.Status == ValidationStatus.Failed))
                return ValidationStatus.Failed;
            
            return ValidationStatus.Passed;
        }

        private async Task<Dictionary<string, string>> ParsePackageVersionsAsync(string packagesPropsPath)
        {
            // Implementation for parsing Directory.Packages.props
            var versions = new Dictionary<string, string>();
            // XML parsing logic would go here
            await Task.CompletedTask; // Placeholder for async operation
            return versions;
        }

        private async Task<List<string>> ParseProjectDependenciesAsync(string projectFile)
        {
            // Implementation for parsing project dependencies
            var dependencies = new List<string>();
            // XML parsing logic would go here
            await Task.CompletedTask; // Placeholder for async operation
            return dependencies;
        }

        private void ValidateProjectPackageReferences(string projectFile, List<string> dependencies, 
            Dictionary<string, string> packageVersions, PhaseResult result)
        {
            // Implementation for validating package references
        }

        private void ValidateFrameworkCompatibility(Dictionary<string, string> packageVersions, PhaseResult result)
        {
            // Implementation for framework compatibility validation
        }

        private async Task ValidatePackageRestoreAsync(PhaseResult result)
        {
            // Implementation for package restore validation
            await Task.CompletedTask; // Placeholder for async operation
        }

        private async Task<Dictionary<string, List<string>>> BuildDependencyGraphAsync(List<string> projectFiles)
        {
            // Implementation for building dependency graph
            var graph = new Dictionary<string, List<string>>();
            await Task.CompletedTask; // Placeholder for async operation
            return graph;
        }

        private List<List<string>> DetectCircularDependencies(Dictionary<string, List<string>> dependencyGraph)
        {
            // Implementation for circular dependency detection
            return new List<List<string>>();
        }

        private void ValidateBuildOrder(Dictionary<string, List<string>> dependencyGraph, PhaseResult result)
        {
            // Implementation for build order validation
        }

        private List<string> GetOptimalBuildOrder()
        {
            // Implementation returns build order per design document
            return new List<string>
            {
                Path.Combine(_workspaceRoot, "src/Inventory.Shared/Inventory.Shared.csproj"),
                Path.Combine(_workspaceRoot, "src/Inventory.UI/Inventory.UI.csproj"),
                Path.Combine(_workspaceRoot, "src/Inventory.Web.Assets/Inventory.Web.Assets.csproj"),
                Path.Combine(_workspaceRoot, "src/Inventory.Web.Client/Inventory.Web.Client.csproj"),
                Path.Combine(_workspaceRoot, "src/Inventory.API/Inventory.API.csproj")
            };
        }

        private async Task<CompilationResult> CompileProjectAsync(string projectPath)
        {
            // Implementation for project compilation
            var result = new CompilationResult();
            await Task.CompletedTask; // Placeholder for async operation
            return result;
        }

        private async Task ValidateDockerfileAsync(string dockerfile, PhaseResult result)
        {
            // Implementation for Dockerfile validation
            await Task.CompletedTask; // Placeholder for async operation
        }

        private async Task ValidateDockerComposeAsync(PhaseResult result)
        {
            // Implementation for docker-compose validation
            await Task.CompletedTask; // Placeholder for async operation
        }

        private async Task ValidateConfigurationFilesAsync(PhaseResult result)
        {
            // Implementation for configuration file validation
            await Task.CompletedTask; // Placeholder for async operation
        }

        #endregion
    }
}