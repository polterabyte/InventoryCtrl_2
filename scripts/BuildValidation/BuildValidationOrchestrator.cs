using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Main orchestrator for build validation system implementing design document
    /// Coordinates all validation, diagnostics, and testing components
    /// </summary>
    public class BuildValidationOrchestrator
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;
        private readonly BuildValidator _buildValidator;
        private readonly CompilationErrorResolver _errorResolver;
        private readonly DockerBuildDiagnostics _dockerDiagnostics;
        private readonly EnvironmentValidator _environmentValidator;
        private readonly TestingFramework _testingFramework;
        private readonly MonitoringSystem _monitoringSystem;

        public BuildValidationOrchestrator(string workspaceRoot)
        {
            _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
            _logger = new ConsoleLogger(enableDebug: true);
            
            var errorClassifier = new ErrorClassifier();
            
            _buildValidator = new BuildValidator(_workspaceRoot, errorClassifier, _logger);
            _errorResolver = new CompilationErrorResolver(_workspaceRoot, _logger, errorClassifier);
            _dockerDiagnostics = new DockerBuildDiagnostics(_workspaceRoot, _logger, errorClassifier);
            _environmentValidator = new EnvironmentValidator(_workspaceRoot, _logger);
            _testingFramework = new TestingFramework(_workspaceRoot, _logger);
            _monitoringSystem = new MonitoringSystem(_workspaceRoot, _logger);
        }

        /// <summary>
        /// Executes complete build validation workflow per design document
        /// </summary>
        public async Task<OrchestrationResult> ExecuteCompleteValidationAsync()
        {
            var result = new OrchestrationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo("Starting complete build validation orchestration");

                // Phase 1: Build Validation
                _logger.LogInfo("Phase 1: Build Validation");
                var buildResult = await _buildValidator.ValidateAsync();
                result.BuildValidationResult = buildResult;

                // Phase 2: Error Resolution (if needed)
                if (buildResult.OverallStatus != ValidationStatus.Passed)
                {
                    _logger.LogInfo("Phase 2: Error Resolution");
                    var errors = buildResult.GetAllErrors().ToList();
                    var resolutionResult = await _errorResolver.ResolveErrorsAsync(errors);
                    result.ErrorResolutionResult = resolutionResult;
                }

                // Phase 3: Docker Validation
                _logger.LogInfo("Phase 3: Docker Validation");
                var dockerResult = await _dockerDiagnostics.ValidateDockerBuildsAsync();
                result.DockerValidationResult = dockerResult;

                // Phase 4: Environment Validation
                _logger.LogInfo("Phase 4: Environment Validation");
                var envResult = await _environmentValidator.ValidateEnvironmentAsync();
                result.EnvironmentValidationResult = envResult;

                // Phase 5: Testing
                _logger.LogInfo("Phase 5: Comprehensive Testing");
                var testResult = await _testingFramework.RunComprehensiveTestSuiteAsync();
                result.TestingResult = testResult;

                // Phase 6: Monitoring Setup
                _logger.LogInfo("Phase 6: Monitoring and Diagnostics");
                var monitoringResult = await _monitoringSystem.SetupMonitoringAsync();
                result.MonitoringResult = monitoringResult;

                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = DetermineOverallStatus(result);

                // Generate comprehensive report
                var report = await GenerateValidationReportAsync(result);
                result.Report = report;

                _logger.LogInfo($"Complete validation orchestration completed in {result.Duration.TotalSeconds:F2}s with status: {result.OverallStatus}");
            }
            catch (Exception ex)
            {
                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = ValidationStatus.Error;
                result.Exception = ex;
                _logger.LogError($"Orchestration failed: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Executes targeted validation for specific components
        /// </summary>
        public async Task<OrchestrationResult> ExecuteTargetedValidationAsync(ValidationTarget target)
        {
            var result = new OrchestrationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo($"Starting targeted validation for: {target}");

                switch (target)
                {
                    case ValidationTarget.Build:
                        result.BuildValidationResult = await _buildValidator.ValidateAsync();
                        break;
                    
                    case ValidationTarget.Docker:
                        result.DockerValidationResult = await _dockerDiagnostics.ValidateDockerBuildsAsync();
                        break;
                    
                    case ValidationTarget.Environment:
                        result.EnvironmentValidationResult = await _environmentValidator.ValidateEnvironmentAsync();
                        break;
                    
                    case ValidationTarget.Testing:
                        result.TestingResult = await _testingFramework.RunComprehensiveTestSuiteAsync();
                        break;
                    
                    case ValidationTarget.Monitoring:
                        result.MonitoringResult = await _monitoringSystem.SetupMonitoringAsync();
                        break;
                }

                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = DetermineTargetedStatus(result, target);

                _logger.LogInfo($"Targeted validation completed in {result.Duration.TotalSeconds:F2}s with status: {result.OverallStatus}");
            }
            catch (Exception ex)
            {
                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = ValidationStatus.Error;
                result.Exception = ex;
                _logger.LogError($"Targeted validation failed: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Executes error diagnosis and resolution workflow
        /// </summary>
        public async Task<DiagnosisResult> DiagnoseAndResolveErrorsAsync(string errorLog)
        {
            var result = new DiagnosisResult();

            try
            {
                _logger.LogInfo("Starting error diagnosis and resolution");

                // Parse errors from log
                var errors = ParseErrorsFromLog(errorLog);
                
                // Classify errors
                var errorClassifier = new ErrorClassifier();
                var classifiedErrors = new List<BuildError>();
                
                foreach (var error in errors)
                {
                    var classified = errorClassifier.ClassifyCompilationError(error);
                    classifiedErrors.Add(classified);
                }

                // Attempt resolution
                var resolutionResult = await _errorResolver.ResolveErrorsAsync(classifiedErrors);
                result.ResolutionResult = resolutionResult;

                // Generate diagnosis report
                result.DiagnosisReport = GenerateDiagnosisReport(classifiedErrors, resolutionResult);
                result.Success = resolutionResult.SuccessRate > 0.5; // 50% resolution rate threshold

                _logger.LogInfo($"Error diagnosis completed. Resolution rate: {resolutionResult.SuccessRate:P2}");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Success = false;
                _logger.LogError($"Error diagnosis failed: {ex.Message}", ex);
            }

            return result;
        }

        private ValidationStatus DetermineOverallStatus(OrchestrationResult result)
        {
            var statuses = new List<ValidationStatus>();

            if (result.BuildValidationResult != null)
                statuses.Add(result.BuildValidationResult.OverallStatus);
            
            if (result.DockerValidationResult != null)
                statuses.Add(result.DockerValidationResult.OverallStatus);
            
            if (result.EnvironmentValidationResult != null)
                statuses.Add(result.EnvironmentValidationResult.OverallStatus);

            if (result.TestingResult != null)
                statuses.Add(ConvertTestStatusToValidationStatus(result.TestingResult.OverallStatus));

            if (statuses.Contains(ValidationStatus.Error))
                return ValidationStatus.Error;
            
            if (statuses.Contains(ValidationStatus.Failed))
                return ValidationStatus.Failed;
            
            return ValidationStatus.Passed;
        }

        private ValidationStatus DetermineTargetedStatus(OrchestrationResult result, ValidationTarget target)
        {
            return target switch
            {
                ValidationTarget.Build => result.BuildValidationResult?.OverallStatus ?? ValidationStatus.Skipped,
                ValidationTarget.Docker => result.DockerValidationResult?.OverallStatus ?? ValidationStatus.Skipped,
                ValidationTarget.Environment => result.EnvironmentValidationResult?.OverallStatus ?? ValidationStatus.Skipped,
                ValidationTarget.Testing => ConvertTestStatusToValidationStatus(result.TestingResult?.OverallStatus ?? TestStatus.Skipped),
                ValidationTarget.Monitoring => result.MonitoringResult?.OverallStatus ?? MonitoringStatus.Unknown,
                _ => ValidationStatus.Skipped
            };
        }

        private ValidationStatus ConvertTestStatusToValidationStatus(TestStatus testStatus)
        {
            return testStatus switch
            {
                TestStatus.Passed => ValidationStatus.Passed,
                TestStatus.Failed => ValidationStatus.Failed,
                TestStatus.Error => ValidationStatus.Error,
                TestStatus.Skipped => ValidationStatus.Skipped,
                _ => ValidationStatus.Skipped
            };
        }

        private async Task<ValidationReport> GenerateValidationReportAsync(OrchestrationResult result)
        {
            var report = new ValidationReport
            {
                Timestamp = DateTime.UtcNow,
                OverallStatus = result.OverallStatus,
                Duration = result.Duration,
                WorkspaceRoot = _workspaceRoot
            };

            // Add section summaries
            if (result.BuildValidationResult != null)
            {
                report.Sections.Add("Build Validation", new ReportSection
                {
                    Status = result.BuildValidationResult.OverallStatus,
                    Summary = $"Validated {result.BuildValidationResult.PhaseResults.Count} phases",
                    ErrorCount = result.BuildValidationResult.GetAllErrors().Count(),
                    Details = JsonSerializer.Serialize(result.BuildValidationResult, new JsonSerializerOptions { WriteIndented = true })
                });
            }

            if (result.DockerValidationResult != null)
            {
                report.Sections.Add("Docker Validation", new ReportSection
                {
                    Status = result.DockerValidationResult.OverallStatus,
                    Summary = $"Validated {result.DockerValidationResult.DockerfileResults.Count} Dockerfiles",
                    ErrorCount = result.DockerValidationResult.Errors.Count,
                    Details = $"Dockerfiles: {result.DockerValidationResult.DockerfileResults.Count}, Compose files: {result.DockerValidationResult.ComposeResults.Count}"
                });
            }

            if (result.EnvironmentValidationResult != null)
            {
                var validationStatus = result.EnvironmentValidationResult.OverallStatus;
                report.Sections.Add("Environment Validation", new ReportSection
                {
                    Status = validationStatus,
                    Summary = $"Validated {result.EnvironmentValidationResult.ValidationResults.Count} components",
                    ErrorCount = result.EnvironmentValidationResult.Errors.Count,
                    Details = string.Join(", ", result.EnvironmentValidationResult.ValidationResults.Keys)
                });
            }

            if (result.TestingResult != null)
            {
                var testStatus = ConvertTestStatusToValidationStatus(result.TestingResult.OverallStatus);
                report.Sections.Add("Testing", new ReportSection
                {
                    Status = testStatus,
                    Summary = $"Executed {result.TestingResult.TestResults.Count} test types",
                    ErrorCount = result.TestingResult.TestResults.Values.SelectMany(r => r.FailedProjects).Count(),
                    Details = string.Join(", ", result.TestingResult.TestResults.Keys)
                });
            }

            await Task.CompletedTask; // Placeholder for async operations
            return report;
        }

        private List<string> ParseErrorsFromLog(string errorLog)
        {
            var errors = new List<string>();
            var lines = errorLog.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("exception", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(line.Trim());
                }
            }

            return errors;
        }

        private string GenerateDiagnosisReport(List<BuildError> errors, CompilationResolutionResult resolutionResult)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== ERROR DIAGNOSIS REPORT ===");
            report.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total Errors: {errors.Count}");
            report.AppendLine($"Resolved Errors: {resolutionResult.ResolvedErrors.Count}");
            report.AppendLine($"Resolution Rate: {resolutionResult.SuccessRate:P2}");
            report.AppendLine();

            // Error breakdown by category
            var errorsByCategory = errors.GroupBy(e => e.Category);
            report.AppendLine("Error Breakdown by Category:");
            foreach (var group in errorsByCategory)
            {
                report.AppendLine($"  {group.Key}: {group.Count()} errors");
            }
            report.AppendLine();

            // Resolution actions
            report.AppendLine("Resolution Actions Taken:");
            foreach (var categoryResult in resolutionResult.CategoryResults)
            {
                report.AppendLine($"  {categoryResult.Key}:");
                foreach (var action in categoryResult.Value.ResolutionActions)
                {
                    report.AppendLine($"    - {action}");
                }
            }

            return report.ToString();
        }
    }

    #region Supporting Classes

    public enum ValidationTarget
    {
        Build,
        Docker,
        Environment,
        Testing,
        Monitoring
    }

    public class OrchestrationResult
    {
        public ValidationStatus OverallStatus { get; set; }
        public TimeSpan Duration { get; set; }
        public BuildValidationResult? BuildValidationResult { get; set; }
        public CompilationResolutionResult? ErrorResolutionResult { get; set; }
        public DockerValidationResult? DockerValidationResult { get; set; }
        public EnvironmentValidationResult? EnvironmentValidationResult { get; set; }
        public TestSuiteResult? TestingResult { get; set; }
        public MonitoringSetupResult? MonitoringResult { get; set; }
        public ValidationReport? Report { get; set; }
        public Exception? Exception { get; set; }
    }

    public class DiagnosisResult
    {
        public bool Success { get; set; }
        public CompilationResolutionResult? ResolutionResult { get; set; }
        public string DiagnosisReport { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    public class ValidationReport
    {
        public DateTime Timestamp { get; set; }
        public ValidationStatus OverallStatus { get; set; }
        public TimeSpan Duration { get; set; }
        public string WorkspaceRoot { get; set; } = string.Empty;
        public Dictionary<string, ReportSection> Sections { get; } = new();
    }

    public class ReportSection
    {
        public ValidationStatus Status { get; set; }
        public string Summary { get; set; } = string.Empty;
        public int ErrorCount { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    #endregion
}