using System;
using System.Collections.Generic;
using System.Linq;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Error classifier implementing the error categorization from design document
    /// </summary>
    public interface IErrorClassifier
    {
        BuildError ClassifyCompilationError(string errorMessage);
        BuildError ClassifyDockerError(string errorMessage);
        BuildError ClassifyRuntimeError(Exception exception);
        ErrorCategory DetermineCategory(string errorMessage);
        ErrorSeverity DetermineSeverity(string errorMessage, ErrorCategory category);
    }

    public class ErrorClassifier : IErrorClassifier
    {
        private readonly Dictionary<string, ErrorCategory> _errorPatterns;
        private readonly Dictionary<ErrorCategory, ErrorSeverity> _defaultSeverities;

        public ErrorClassifier()
        {
            _errorPatterns = InitializeErrorPatterns();
            _defaultSeverities = InitializeDefaultSeverities();
        }

        public BuildError ClassifyCompilationError(string errorMessage)
        {
            var category = DetermineCategory(errorMessage);
            var severity = DetermineSeverity(errorMessage, category);

            return new BuildError
            {
                Category = category,
                Message = errorMessage,
                Source = "Compilation",
                Severity = severity,
                Timestamp = DateTime.UtcNow,
                ResolutionStrategy = GetResolutionStrategy(category, errorMessage)
            };
        }

        public BuildError ClassifyDockerError(string errorMessage)
        {
            var category = ErrorCategory.DockerBuild;
            var severity = DetermineSeverity(errorMessage, category);

            // Docker-specific error classification
            if (errorMessage.Contains("COPY failed") || errorMessage.Contains("no such file"))
                category = ErrorCategory.DockerBuild;
            else if (errorMessage.Contains("network") || errorMessage.Contains("timeout"))
                category = ErrorCategory.NetworkConnectivity;
            else if (errorMessage.Contains("permission denied"))
                category = ErrorCategory.EnvironmentConfiguration;

            return new BuildError
            {
                Category = category,
                Message = errorMessage,
                Source = "Docker",
                Severity = severity,
                Timestamp = DateTime.UtcNow,
                ResolutionStrategy = GetResolutionStrategy(category, errorMessage)
            };
        }

        public BuildError ClassifyRuntimeError(Exception exception)
        {
            var category = DetermineRuntimeErrorCategory(exception);
            var severity = DetermineRuntimeErrorSeverity(exception, category);

            return new BuildError
            {
                Category = category,
                Message = exception.Message,
                Source = "Runtime",
                Severity = severity,
                Timestamp = DateTime.UtcNow,
                Exception = exception,
                ResolutionStrategy = GetResolutionStrategy(category, exception.Message)
            };
        }

        public ErrorCategory DetermineCategory(string errorMessage)
        {
            var message = errorMessage.ToLowerInvariant();

            foreach (var pattern in _errorPatterns)
            {
                if (message.Contains(pattern.Key.ToLowerInvariant()))
                {
                    return pattern.Value;
                }
            }

            // Default categorization based on common patterns
            if (message.Contains("reference") || message.Contains("assembly"))
                return ErrorCategory.ProjectReference;
            if (message.Contains("package") || message.Contains("nuget"))
                return ErrorCategory.PackageReference;
            if (message.Contains("syntax") || message.Contains("expected"))
                return ErrorCategory.CompilationError;
            if (message.Contains("framework") || message.Contains("target"))
                return ErrorCategory.FrameworkCompatibility;

            return ErrorCategory.Unknown;
        }

        public ErrorSeverity DetermineSeverity(string errorMessage, ErrorCategory category)
        {
            var message = errorMessage.ToLowerInvariant();

            // Critical severity patterns
            if (message.Contains("fatal") || message.Contains("critical") || 
                message.Contains("circular dependency") || message.Contains("missing assembly"))
                return ErrorSeverity.Critical;

            // High severity patterns
            if (message.Contains("error") || message.Contains("failed") || 
                message.Contains("cannot resolve") || message.Contains("not found"))
                return ErrorSeverity.High;

            // Medium severity patterns
            if (message.Contains("warning") || message.Contains("deprecated"))
                return ErrorSeverity.Medium;

            // Default based on category
            return _defaultSeverities.GetValueOrDefault(category, ErrorSeverity.Medium);
        }

        private ErrorCategory DetermineRuntimeErrorCategory(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => ErrorCategory.Authentication,
                InvalidOperationException when exception.Message.Contains("JWT") => ErrorCategory.Authentication,
                TimeoutException => ErrorCategory.NetworkConnectivity,
                System.Data.Common.DbException => ErrorCategory.DatabaseConnectivity,
                System.Net.Http.HttpRequestException => ErrorCategory.NetworkConnectivity,
                ArgumentNullException => ErrorCategory.ConfigurationError,
                FileNotFoundException => ErrorCategory.EnvironmentConfiguration,
                DirectoryNotFoundException => ErrorCategory.EnvironmentConfiguration,
                _ => ErrorCategory.RuntimeException
            };
        }

        private ErrorSeverity DetermineRuntimeErrorSeverity(Exception exception, ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.Authentication => ErrorSeverity.High,
                ErrorCategory.DatabaseConnectivity => ErrorSeverity.Critical,
                ErrorCategory.NetworkConnectivity => ErrorSeverity.High,
                ErrorCategory.ConfigurationError => ErrorSeverity.High,
                ErrorCategory.EnvironmentConfiguration => ErrorSeverity.Medium,
                _ => ErrorSeverity.Medium
            };
        }

        private string GetResolutionStrategy(ErrorCategory category, string errorMessage)
        {
            return category switch
            {
                ErrorCategory.PackageReference => "Check Directory.Packages.props for version conflicts and update package references",
                ErrorCategory.ProjectReference => "Verify project reference paths and resolve circular dependencies",
                ErrorCategory.CompilationError => "Review syntax errors and missing using statements",
                ErrorCategory.FrameworkCompatibility => "Ensure all projects target .NET 8.0 framework",
                ErrorCategory.DockerBuild => "Validate Dockerfile syntax and verify file paths in COPY instructions",
                ErrorCategory.DatabaseConnectivity => "Check connection string configuration and database availability",
                ErrorCategory.Authentication => "Verify JWT configuration in appsettings and environment variables",
                ErrorCategory.NetworkConnectivity => "Check network connectivity and firewall settings",
                ErrorCategory.EnvironmentConfiguration => "Validate required environment variables are set correctly",
                ErrorCategory.ConfigurationError => "Review appsettings.json files and environment-specific configurations",
                _ => "Review error details and consult documentation for resolution steps"
            };
        }

        private Dictionary<string, ErrorCategory> InitializeErrorPatterns()
        {
            return new Dictionary<string, ErrorCategory>
            {
                // Package Reference Errors
                ["package"] = ErrorCategory.PackageReference,
                ["nuget"] = ErrorCategory.PackageReference,
                ["version conflict"] = ErrorCategory.PackageReference,
                ["restore failed"] = ErrorCategory.PackageReference,

                // Project Reference Errors
                ["project reference"] = ErrorCategory.ProjectReference,
                ["circular dependency"] = ErrorCategory.ProjectReference,
                ["assembly reference"] = ErrorCategory.ProjectReference,

                // Compilation Errors
                ["syntax error"] = ErrorCategory.CompilationError,
                ["expected"] = ErrorCategory.CompilationError,
                ["missing using"] = ErrorCategory.CompilationError,
                ["namespace"] = ErrorCategory.CompilationError,

                // Framework Compatibility
                ["target framework"] = ErrorCategory.FrameworkCompatibility,
                ["framework version"] = ErrorCategory.FrameworkCompatibility,
                ["net8.0"] = ErrorCategory.FrameworkCompatibility,

                // Docker Build Errors
                ["dockerfile"] = ErrorCategory.DockerBuild,
                ["copy failed"] = ErrorCategory.DockerBuild,
                ["no such file"] = ErrorCategory.DockerBuild,
                ["image build"] = ErrorCategory.DockerBuild,

                // Database Connectivity
                ["connection string"] = ErrorCategory.DatabaseConnectivity,
                ["postgres"] = ErrorCategory.DatabaseConnectivity,
                ["database"] = ErrorCategory.DatabaseConnectivity,
                ["sql"] = ErrorCategory.DatabaseConnectivity,

                // Authentication
                ["jwt"] = ErrorCategory.Authentication,
                ["unauthorized"] = ErrorCategory.Authentication,
                ["token"] = ErrorCategory.Authentication,
                ["authentication"] = ErrorCategory.Authentication,

                // Network Connectivity
                ["timeout"] = ErrorCategory.NetworkConnectivity,
                ["network"] = ErrorCategory.NetworkConnectivity,
                ["connection refused"] = ErrorCategory.NetworkConnectivity,
                ["httprequest"] = ErrorCategory.NetworkConnectivity,

                // Environment Configuration
                ["environment variable"] = ErrorCategory.EnvironmentConfiguration,
                ["appsettings"] = ErrorCategory.ConfigurationError,
                ["configuration"] = ErrorCategory.ConfigurationError,
                ["ssl"] = ErrorCategory.EnvironmentConfiguration
            };
        }

        private Dictionary<ErrorCategory, ErrorSeverity> InitializeDefaultSeverities()
        {
            return new Dictionary<ErrorCategory, ErrorSeverity>
            {
                [ErrorCategory.CompilationError] = ErrorSeverity.High,
                [ErrorCategory.PackageReference] = ErrorSeverity.High,
                [ErrorCategory.ProjectReference] = ErrorSeverity.Critical,
                [ErrorCategory.FrameworkCompatibility] = ErrorSeverity.High,
                [ErrorCategory.DockerBuild] = ErrorSeverity.High,
                [ErrorCategory.DatabaseConnectivity] = ErrorSeverity.Critical,
                [ErrorCategory.Authentication] = ErrorSeverity.High,
                [ErrorCategory.NetworkConnectivity] = ErrorSeverity.High,
                [ErrorCategory.EnvironmentConfiguration] = ErrorSeverity.Medium,
                [ErrorCategory.ConfigurationError] = ErrorSeverity.Medium,
                [ErrorCategory.RuntimeException] = ErrorSeverity.Medium,
                [ErrorCategory.SystemError] = ErrorSeverity.Critical,
                [ErrorCategory.Unknown] = ErrorSeverity.Low
            };
        }
    }

    /// <summary>
    /// Error categories as defined in design document
    /// </summary>
    public enum ErrorCategory
    {
        Unknown,
        CompilationError,
        PackageReference,
        ProjectReference,
        FrameworkCompatibility,
        DockerBuild,
        DatabaseConnectivity,
        Authentication,
        NetworkConnectivity,
        EnvironmentConfiguration,
        ConfigurationError,
        RuntimeException,
        SystemError
    }

    /// <summary>
    /// Error severity levels for escalation matrix from design document
    /// </summary>
    public enum ErrorSeverity
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Validation status enumeration
    /// </summary>
    public enum ValidationStatus
    {
        Passed,
        Failed,
        Error,
        Skipped
    }

    /// <summary>
    /// Build error data structure
    /// </summary>
    public class BuildError
    {
        public ErrorCategory Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public ErrorSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public Exception? Exception { get; set; }
        public string ResolutionStrategy { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// Phase result for individual validation phases
    /// </summary>
    public class PhaseResult
    {
        public string Phase { get; }
        public ValidationStatus Status { get; set; }
        public List<BuildError> Errors { get; }
        public List<string> Warnings { get; }
        public DateTime StartTime { get; }
        public TimeSpan Duration { get; set; }

        public PhaseResult(string phase)
        {
            Phase = phase;
            Status = ValidationStatus.Passed;
            Errors = new List<BuildError>();
            Warnings = new List<string>();
            StartTime = DateTime.UtcNow;
        }

        public void AddError(BuildError error)
        {
            Errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// Overall build validation result
    /// </summary>
    public class BuildValidationResult
    {
        public ValidationStatus OverallStatus { get; set; }
        public List<PhaseResult> PhaseResults { get; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; }

        public BuildValidationResult()
        {
            PhaseResults = new List<PhaseResult>();
            StartTime = DateTime.UtcNow;
            OverallStatus = ValidationStatus.Passed;
        }

        public void AddPhaseResult(string phase, PhaseResult result)
        {
            result.Duration = DateTime.UtcNow - result.StartTime;
            PhaseResults.Add(result);
        }

        public void AddError(BuildError error)
        {
            var systemPhase = PhaseResults.FirstOrDefault(p => p.Phase == "System") 
                ?? new PhaseResult("System");
            
            if (!PhaseResults.Contains(systemPhase))
                PhaseResults.Add(systemPhase);
                
            systemPhase.AddError(error);
        }

        public IEnumerable<BuildError> GetAllErrors()
        {
            return PhaseResults.SelectMany(p => p.Errors);
        }

        public IEnumerable<BuildError> GetCriticalErrors()
        {
            return GetAllErrors().Where(e => e.Severity == ErrorSeverity.Critical);
        }

        public IEnumerable<BuildError> GetHighSeverityErrors()
        {
            return GetAllErrors().Where(e => e.Severity >= ErrorSeverity.High);
        }
    }

    /// <summary>
    /// Compilation result structure
    /// </summary>
    public class CompilationResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string Output { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Build configuration structure
    /// </summary>
    public class BuildConfiguration
    {
        public bool EnableParallelBuild { get; set; } = true;
        public TimeSpan BuildTimeout { get; set; } = TimeSpan.FromMinutes(10);
        public List<string> ExcludedProjects { get; set; } = new();
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public bool ValidateDockerBuilds { get; set; } = true;
        public bool ValidateEnvironmentConfig { get; set; } = true;
    }
}