using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Docker build failure diagnostics system implementing strategies from design document
    /// </summary>
    public class DockerBuildDiagnostics
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;
        private readonly IErrorClassifier _errorClassifier;

        public DockerBuildDiagnostics(string workspaceRoot, ILogger logger, IErrorClassifier errorClassifier)
        {
            _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorClassifier = errorClassifier ?? throw new ArgumentNullException(nameof(errorClassifier));
        }

        /// <summary>
        /// Validates all Docker build configurations and identifies potential issues
        /// </summary>
        public async Task<DockerValidationResult> ValidateDockerBuildsAsync()
        {
            var result = new DockerValidationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo("Starting Docker build validation");

                await ValidateDockerfilesAsync(result);
                await ValidateDockerComposeConfigurationsAsync(result);
                await ValidateBuildContextAsync(result);
                await TestDockerBuildsAsync(result);

                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = DetermineOverallStatus(result);

                _logger.LogInfo($"Docker validation completed in {result.Duration.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                result.Duration = DateTime.UtcNow - startTime;
                result.OverallStatus = ValidationStatus.Error;
                result.AddError(CreateSystemError(ex.Message, ex));
                _logger.LogError($"Docker validation failed: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Diagnoses specific Docker build failure from build output
        /// </summary>
        public async Task<DockerBuildDiagnosis> DiagnoseBuildFailureAsync(string dockerfilePath, string buildOutput)
        {
            var diagnosis = new DockerBuildDiagnosis(dockerfilePath);

            try
            {
                _logger.LogInfo($"Diagnosing Docker build failure for: {Path.GetFileName(dockerfilePath)}");

                diagnosis.BuildErrors.AddRange(ParseBuildOutput(buildOutput));
                diagnosis.FailureStage = DetermineFailureStage(buildOutput);
                diagnosis.ResolutionRecommendations.AddRange(GenerateRecommendations(diagnosis.FailureStage));

                var dockerfileAnalysis = await AnalyzeDockerfileAsync(dockerfilePath);
                diagnosis.DockerfileIssues.AddRange(dockerfileAnalysis.Issues);

                diagnosis.DiagnosisCompleted = true;
                _logger.LogInfo($"Docker diagnosis completed for {Path.GetFileName(dockerfilePath)}");
            }
            catch (Exception ex)
            {
                diagnosis.Exception = ex;
                _logger.LogError($"Docker diagnosis failed: {ex.Message}", ex);
            }

            return diagnosis;
        }

        private async Task ValidateDockerfilesAsync(DockerValidationResult result)
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
                    var analysis = await AnalyzeDockerfileAsync(dockerfile);
                    result.DockerfileResults[dockerfile] = analysis;

                    foreach (var issue in analysis.Issues)
                    {
                        result.AddError(CreateDockerError(DockerBuildStage.Dockerfile, issue, dockerfile));
                    }
                }
                else
                {
                    result.AddError(CreateDockerError(DockerBuildStage.Dockerfile, 
                        $"Dockerfile not found: {dockerfile}", dockerfile, ErrorSeverity.High));
                }
            }
        }

        private async Task ValidateDockerComposeConfigurationsAsync(DockerValidationResult result)
        {
            var composeFiles = Directory.GetFiles(_workspaceRoot, "docker-compose*.yml");

            foreach (var composeFile in composeFiles)
            {
                var analysis = await AnalyzeDockerComposeAsync(composeFile);
                result.ComposeResults[composeFile] = analysis;

                foreach (var issue in analysis.Issues)
                {
                    result.AddError(CreateDockerError(DockerBuildStage.Compose, issue, composeFile));
                }
            }
        }

        private async Task ValidateBuildContextAsync(DockerValidationResult result)
        {
            foreach (var dockerfile in result.DockerfileResults.Keys)
            {
                var contextValidation = await ValidateDockerBuildContextAsync(dockerfile);
                if (!contextValidation.IsValid)
                {
                    foreach (var issue in contextValidation.Issues)
                    {
                        result.AddError(CreateDockerError(DockerBuildStage.BuildContext, issue, dockerfile));
                    }
                }
            }
        }

        private async Task TestDockerBuildsAsync(DockerValidationResult result)
        {
            if (!IsDockerAvailable())
            {
                result.AddError(CreateDockerError(DockerBuildStage.Testing, 
                    "Docker is not available for build testing", "Docker", ErrorSeverity.High));
                return;
            }

            foreach (var dockerfile in result.DockerfileResults.Keys)
            {
                try
                {
                    var testResult = await TestDockerBuildAsync(dockerfile);
                    result.BuildTestResults[dockerfile] = testResult;

                    if (!testResult.Success)
                    {
                        result.AddError(CreateDockerError(DockerBuildStage.Testing,
                            $"Build test failed: {testResult.ErrorMessage}", dockerfile, ErrorSeverity.High));
                    }
                }
                catch (Exception ex)
                {
                    result.AddError(CreateSystemError($"Build test exception: {ex.Message}", ex));
                }
            }
        }

        private async Task<DockerfileAnalysisResult> AnalyzeDockerfileAsync(string dockerfilePath)
        {
            var result = new DockerfileAnalysisResult(dockerfilePath);

            try
            {
                var content = await File.ReadAllTextAsync(dockerfilePath);
                var lines = content.Split('\n').Select(l => l.Trim()).ToList();

                ValidateBaseImage(lines, result);
                ValidateCopyInstructions(lines, dockerfilePath, result);
                ValidateDockerfileBestPractices(lines, result);

                result.AnalysisCompleted = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"Analysis failed: {ex.Message}");
            }

            return result;
        }

        private async Task<DockerComposeAnalysisResult> AnalyzeDockerComposeAsync(string composeFilePath)
        {
            var result = new DockerComposeAnalysisResult(composeFilePath);

            try
            {
                var content = await File.ReadAllTextAsync(composeFilePath);
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    result.Issues.Add("Docker Compose file is empty");
                    return result;
                }

                // Basic validation
                if (!content.Contains("version:"))
                    result.Issues.Add("Missing version specification");
                
                if (!content.Contains("services:"))
                    result.Issues.Add("Missing services section");

                result.AnalysisCompleted = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"Analysis failed: {ex.Message}");
            }

            return result;
        }

        private List<DockerBuildError> ParseBuildOutput(string buildOutput)
        {
            var errors = new List<DockerBuildError>();
            var lines = buildOutput.Split('\n');

            var errorPatterns = new Dictionary<string, ErrorCategory>
            {
                ["ERROR"] = ErrorCategory.DockerBuild,
                ["COPY failed"] = ErrorCategory.DockerBuild,
                ["No such file"] = ErrorCategory.DockerBuild,
                ["permission denied"] = ErrorCategory.EnvironmentConfiguration,
                ["network timeout"] = ErrorCategory.NetworkConnectivity
            };

            foreach (var line in lines)
            {
                foreach (var pattern in errorPatterns)
                {
                    if (line.Contains(pattern.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(new DockerBuildError
                        {
                            Stage = DockerBuildStage.Unknown,
                            Category = pattern.Value,
                            Message = line.Trim(),
                            Source = "Docker Build Output",
                            Severity = DetermineSeverityFromOutput(line)
                        });
                        break;
                    }
                }
            }

            return errors;
        }

        private DockerBuildStage DetermineFailureStage(string buildOutput)
        {
            if (buildOutput.Contains("FROM")) return DockerBuildStage.BaseImage;
            if (buildOutput.Contains("restore")) return DockerBuildStage.Restore;
            if (buildOutput.Contains("build")) return DockerBuildStage.Build;
            if (buildOutput.Contains("publish")) return DockerBuildStage.Publish;
            if (buildOutput.Contains("ENTRYPOINT")) return DockerBuildStage.Runtime;
            return DockerBuildStage.Unknown;
        }

        private List<string> GenerateRecommendations(DockerBuildStage stage)
        {
            return stage switch
            {
                DockerBuildStage.BaseImage => new() { "Verify base image availability and version" },
                DockerBuildStage.Restore => new() { "Check NuGet sources and network connectivity" },
                DockerBuildStage.Build => new() { "Review compilation errors and file paths" },
                DockerBuildStage.Publish => new() { "Verify output paths and permissions" },
                DockerBuildStage.Runtime => new() { "Check ENTRYPOINT and runtime configuration" },
                _ => new() { "Review Docker build output for specific errors" }
            };
        }

        private DockerBuildError CreateDockerError(DockerBuildStage stage, string message, string source, 
            ErrorSeverity severity = ErrorSeverity.Medium)
        {
            return new DockerBuildError
            {
                Stage = stage,
                Category = ErrorCategory.DockerBuild,
                Message = message,
                Source = source,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            };
        }

        private DockerBuildError CreateSystemError(string message, Exception exception)
        {
            return new DockerBuildError
            {
                Stage = DockerBuildStage.Validation,
                Category = ErrorCategory.SystemError,
                Message = message,
                Source = "DockerBuildDiagnostics",
                Severity = ErrorSeverity.Critical,
                Exception = exception,
                Timestamp = DateTime.UtcNow
            };
        }

        private ValidationStatus DetermineOverallStatus(DockerValidationResult result)
        {
            if (result.Errors.Any(e => e.Severity == ErrorSeverity.Critical))
                return ValidationStatus.Error;
            if (result.Errors.Any(e => e.Severity >= ErrorSeverity.High))
                return ValidationStatus.Failed;
            return ValidationStatus.Passed;
        }

        private ErrorSeverity DetermineSeverityFromOutput(string outputLine)
        {
            var line = outputLine.ToLowerInvariant();
            if (line.Contains("fatal") || line.Contains("critical")) return ErrorSeverity.Critical;
            if (line.Contains("error")) return ErrorSeverity.High;
            if (line.Contains("warning")) return ErrorSeverity.Medium;
            return ErrorSeverity.Low;
        }

        // Helper methods for validation
        private void ValidateBaseImage(List<string> lines, DockerfileAnalysisResult result)
        {
            var fromLines = lines.Where(l => l.StartsWith("FROM ", StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!fromLines.Any())
            {
                result.Issues.Add("No FROM instruction found");
                return;
            }

            foreach (var fromLine in fromLines)
            {
                var parts = fromLine.Split(' ');
                if (parts.Length < 2)
                {
                    result.Issues.Add($"Invalid FROM instruction: {fromLine}");
                    continue;
                }

                var image = parts[1];
                if (!image.Contains(':'))
                    result.Recommendations.Add($"Consider explicit tag for: {image}");

                if ((image.Contains("dotnet") || image.Contains("aspnet")) && !image.Contains("8.0"))
                    result.Issues.Add($"Should use .NET 8.0: {image}");
            }
        }

        private void ValidateCopyInstructions(List<string> lines, string dockerfilePath, DockerfileAnalysisResult result)
        {
            var copyLines = lines.Where(l => l.StartsWith("COPY ", StringComparison.OrdinalIgnoreCase)).ToList();
            var dockerfileDir = Path.GetDirectoryName(dockerfilePath) ?? string.Empty;

            foreach (var copyLine in copyLines)
            {
                var parts = copyLine.Split(' ');
                if (parts.Length < 3)
                {
                    result.Issues.Add($"Invalid COPY instruction: {copyLine}");
                    continue;
                }

                var source = parts[1];
                if (source.StartsWith("/") || source.Contains(".."))
                    result.Issues.Add($"COPY source should be relative: {source}");

                var sourcePath = Path.Combine(dockerfileDir, source);
                if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath) && !source.Contains("*"))
                    result.Issues.Add($"COPY source not found: {source}");
            }
        }

        private void ValidateDockerfileBestPractices(List<string> lines, DockerfileAnalysisResult result)
        {
            var runCount = lines.Count(l => l.StartsWith("RUN ", StringComparison.OrdinalIgnoreCase));
            if (runCount > 5)
                result.Recommendations.Add("Consider combining RUN instructions to reduce layers");

            if (!lines.Any(l => l.StartsWith("WORKDIR ", StringComparison.OrdinalIgnoreCase)))
                result.Recommendations.Add("Consider using WORKDIR for better organization");
        }

        private async Task<BuildContextValidationResult> ValidateDockerBuildContextAsync(string dockerfilePath)
        {
            var result = new BuildContextValidationResult();
            
            try
            {
                var dockerfileDir = Path.GetDirectoryName(dockerfilePath) ?? string.Empty;
                var dockerignorePath = Path.Combine(dockerfileDir, ".dockerignore");
                
                if (!File.Exists(dockerignorePath))
                    result.Recommendations.Add("Consider adding .dockerignore file");

                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Issues.Add($"Context validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private async Task<DockerBuildTestResult> TestDockerBuildAsync(string dockerfilePath)
        {
            var result = new DockerBuildTestResult(dockerfilePath);

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"build -f {dockerfilePath} .",
                        WorkingDirectory = Path.GetDirectoryName(dockerfilePath),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                var startTime = DateTime.UtcNow;
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                result.Success = process.ExitCode == 0;
                result.Output = output;
                result.ErrorMessage = error;
                result.Duration = DateTime.UtcNow - startTime;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private bool IsDockerAvailable()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    // Supporting classes
    public enum DockerBuildStage
    {
        Unknown,
        Dockerfile,
        Compose,
        BuildContext,
        BaseImage,
        Restore,
        Build,
        Publish,
        Runtime,
        Testing,
        Validation,
        MultiStage
    }

    public class DockerBuildError : BuildError
    {
        public DockerBuildStage Stage { get; set; }
    }

    public class DockerValidationResult
    {
        public ValidationStatus OverallStatus { get; set; }
        public List<DockerBuildError> Errors { get; } = new();
        public Dictionary<string, DockerfileAnalysisResult> DockerfileResults { get; } = new();
        public Dictionary<string, DockerComposeAnalysisResult> ComposeResults { get; } = new();
        public Dictionary<string, DockerBuildTestResult> BuildTestResults { get; } = new();
        public TimeSpan Duration { get; set; }

        public void AddError(DockerBuildError error) => Errors.Add(error);
    }

    public class DockerBuildDiagnosis
    {
        public string DockerfilePath { get; }
        public List<DockerBuildError> BuildErrors { get; } = new();
        public List<string> DockerfileIssues { get; } = new();
        public List<string> ResolutionRecommendations { get; } = new();
        public DockerBuildStage FailureStage { get; set; }
        public bool DiagnosisCompleted { get; set; }
        public Exception? Exception { get; set; }

        public DockerBuildDiagnosis(string dockerfilePath)
        {
            DockerfilePath = dockerfilePath;
        }
    }

    public class DockerfileAnalysisResult
    {
        public string DockerfilePath { get; }
        public List<string> Issues { get; } = new();
        public List<string> Recommendations { get; } = new();
        public bool AnalysisCompleted { get; set; }
        public Exception? Exception { get; set; }

        public DockerfileAnalysisResult(string dockerfilePath)
        {
            DockerfilePath = dockerfilePath;
        }
    }

    public class DockerComposeAnalysisResult
    {
        public string ComposeFilePath { get; }
        public List<string> Issues { get; } = new();
        public List<string> Recommendations { get; } = new();
        public bool AnalysisCompleted { get; set; }
        public Exception? Exception { get; set; }

        public DockerComposeAnalysisResult(string composeFilePath)
        {
            ComposeFilePath = composeFilePath;
        }
    }

    public class BuildContextValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Issues { get; } = new();
        public List<string> Recommendations { get; } = new();
        public Exception? Exception { get; set; }
    }

    public class DockerBuildTestResult
    {
        public string DockerfilePath { get; }
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public Exception? Exception { get; set; }

        public DockerBuildTestResult(string dockerfilePath)
        {
            DockerfilePath = dockerfilePath;
        }
    }
}