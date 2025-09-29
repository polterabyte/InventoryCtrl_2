using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Compilation error resolver implementing strategies from design document
    /// Handles package references, project dependencies, and framework compatibility issues
    /// </summary>
    public class CompilationErrorResolver
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;
        private readonly IErrorClassifier _errorClassifier;
        private readonly Dictionary<ErrorCategory, IErrorResolutionStrategy> _resolutionStrategies;

        public CompilationErrorResolver(string workspaceRoot, ILogger logger, IErrorClassifier errorClassifier)
        {
            _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorClassifier = errorClassifier ?? throw new ArgumentNullException(nameof(errorClassifier));
            _resolutionStrategies = InitializeResolutionStrategies();
        }

        /// <summary>
        /// Attempts to resolve compilation errors automatically
        /// </summary>
        public async Task<CompilationResolutionResult> ResolveErrorsAsync(List<BuildError> errors)
        {
            var result = new CompilationResolutionResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo($"Starting compilation error resolution for {errors.Count} errors");

                // Group errors by category for efficient resolution
                var errorsByCategory = errors.GroupBy(e => e.Category).ToList();

                foreach (var categoryGroup in errorsByCategory)
                {
                    var category = categoryGroup.Key;
                    var categoryErrors = categoryGroup.ToList();

                    _logger.LogInfo($"Resolving {categoryErrors.Count} errors in category: {category}");

                    if (_resolutionStrategies.TryGetValue(category, out var strategy))
                    {
                        var categoryResult = await strategy.ResolveAsync(categoryErrors);
                        result.AddCategoryResult(category, categoryResult);

                        foreach (var resolvedError in categoryResult.ResolvedErrors)
                        {
                            result.ResolvedErrors.Add(resolvedError);
                        }

                        foreach (var unresolvedError in categoryResult.UnresolvedErrors)
                        {
                            result.UnresolvedErrors.Add(unresolvedError);
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No resolution strategy available for category: {category}");
                        result.UnresolvedErrors.AddRange(categoryErrors);
                    }
                }

                result.Duration = DateTime.UtcNow - startTime;
                result.SuccessRate = errors.Count > 0 ? (double)result.ResolvedErrors.Count / errors.Count : 1.0;

                _logger.LogInfo($"Compilation error resolution completed. Resolved: {result.ResolvedErrors.Count}, Unresolved: {result.UnresolvedErrors.Count}");
            }
            catch (Exception ex)
            {
                result.Duration = DateTime.UtcNow - startTime;
                result.Exception = ex;
                _logger.LogError($"Compilation error resolution failed: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Analyzes project for potential compilation issues before building
        /// </summary>
        public async Task<PreCompilationAnalysisResult> AnalyzeProjectAsync(string projectPath)
        {
            var result = new PreCompilationAnalysisResult(projectPath);

            try
            {
                _logger.LogInfo($"Analyzing project for potential issues: {Path.GetFileName(projectPath)}");

                // Analyze project file structure
                await AnalyzeProjectFileAsync(projectPath, result);

                // Analyze package references
                await AnalyzePackageReferencesAsync(projectPath, result);

                // Analyze project references
                await AnalyzeProjectReferencesAsync(projectPath, result);

                // Analyze source files
                await AnalyzeSourceFilesAsync(projectPath, result);

                result.AnalysisCompleted = true;
                _logger.LogInfo($"Project analysis completed for {Path.GetFileName(projectPath)}");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                _logger.LogError($"Project analysis failed for {projectPath}: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Performs automated fixes for common compilation issues
        /// </summary>
        public async Task<AutoFixResult> ApplyAutoFixesAsync(string projectPath)
        {
            var result = new AutoFixResult(projectPath);

            try
            {
                _logger.LogInfo($"Applying auto-fixes to project: {Path.GetFileName(projectPath)}");

                // Fix missing using statements
                var usingFixes = await FixMissingUsingStatementsAsync(projectPath);
                result.AppliedFixes.AddRange(usingFixes);

                // Fix package version conflicts
                var packageFixes = await FixPackageVersionConflictsAsync(projectPath);
                result.AppliedFixes.AddRange(packageFixes);

                // Fix project reference issues
                var referenceFixes = await FixProjectReferenceIssuesAsync(projectPath);
                result.AppliedFixes.AddRange(referenceFixes);

                // Fix nullable reference warnings
                var nullableFixes = await FixNullableReferenceWarningsAsync(projectPath);
                result.AppliedFixes.AddRange(nullableFixes);

                result.Success = true;
                _logger.LogInfo($"Auto-fixes applied successfully. Total fixes: {result.AppliedFixes.Count}");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                _logger.LogError($"Auto-fix application failed for {projectPath}: {ex.Message}", ex);
            }

            return result;
        }

        #region Resolution Strategies

        private Dictionary<ErrorCategory, IErrorResolutionStrategy> InitializeResolutionStrategies()
        {
            return new Dictionary<ErrorCategory, IErrorResolutionStrategy>
            {
                [ErrorCategory.PackageReference] = new PackageReferenceResolutionStrategy(_workspaceRoot, _logger),
                [ErrorCategory.ProjectReference] = new ProjectReferenceResolutionStrategy(_workspaceRoot, _logger),
                [ErrorCategory.CompilationError] = new CompilationErrorResolutionStrategy(_workspaceRoot, _logger),
                [ErrorCategory.FrameworkCompatibility] = new FrameworkCompatibilityResolutionStrategy(_workspaceRoot, _logger),
                [ErrorCategory.ConfigurationError] = new ConfigurationErrorResolutionStrategy(_workspaceRoot, _logger)
            };
        }

        #endregion

        #region Analysis Methods

        private async Task AnalyzeProjectFileAsync(string projectPath, PreCompilationAnalysisResult result)
        {
            if (!File.Exists(projectPath))
            {
                result.Issues.Add($"Project file not found: {projectPath}");
                return;
            }

            var projectDoc = XDocument.Load(projectPath);
            var project = projectDoc.Root;

            if (project == null)
            {
                result.Issues.Add("Invalid project file structure");
                return;
            }

            // Check target framework
            var targetFramework = project.Descendants("TargetFramework").FirstOrDefault()?.Value;
            if (targetFramework != "net8.0")
            {
                result.Issues.Add($"Target framework mismatch. Expected: net8.0, Found: {targetFramework}");
            }

            // Check for nullable reference types
            var nullable = project.Descendants("Nullable").FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(nullable))
            {
                result.Recommendations.Add("Consider enabling nullable reference types: <Nullable>enable</Nullable>");
            }

            await Task.CompletedTask; // Placeholder for async operations
        }

        private async Task AnalyzePackageReferencesAsync(string projectPath, PreCompilationAnalysisResult result)
        {
            var projectDoc = XDocument.Load(projectPath);
            var packageReferences = projectDoc.Descendants("PackageReference").ToList();

            // Load centralized package versions
            var packagesPropsPath = Path.Combine(_workspaceRoot, "Directory.Packages.props");
            var centralizedVersions = new Dictionary<string, string>();

            if (File.Exists(packagesPropsPath))
            {
                var packagesDoc = XDocument.Load(packagesPropsPath);
                centralizedVersions = packagesDoc.Descendants("PackageVersion")
                    .ToDictionary(
                        e => e.Attribute("Include")?.Value ?? string.Empty,
                        e => e.Attribute("Version")?.Value ?? string.Empty
                    );
            }

            foreach (var packageRef in packageReferences)
            {
                var packageName = packageRef.Attribute("Include")?.Value;
                var version = packageRef.Attribute("Version")?.Value;

                if (string.IsNullOrEmpty(packageName)) continue;

                // Check if package should use centralized version management
                if (centralizedVersions.ContainsKey(packageName) && !string.IsNullOrEmpty(version))
                {
                    result.Issues.Add($"Package {packageName} should not specify version (centralized in Directory.Packages.props)");
                }

                // Check for missing centralized version
                if (!centralizedVersions.ContainsKey(packageName))
                {
                    result.Recommendations.Add($"Consider adding {packageName} to Directory.Packages.props for centralized version management");
                }
            }

            await Task.CompletedTask; // Placeholder for async operations
        }

        private async Task AnalyzeProjectReferencesAsync(string projectPath, PreCompilationAnalysisResult result)
        {
            var projectDoc = XDocument.Load(projectPath);
            var projectReferences = projectDoc.Descendants("ProjectReference").ToList();

            foreach (var projectRef in projectReferences)
            {
                var referencePath = projectRef.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(referencePath)) continue;

                var absolutePath = Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, referencePath);
                var normalizedPath = Path.GetFullPath(absolutePath);

                if (!File.Exists(normalizedPath))
                {
                    result.Issues.Add($"Project reference not found: {referencePath}");
                }
            }

            await Task.CompletedTask; // Placeholder for async operations
        }

        private async Task AnalyzeSourceFilesAsync(string projectPath, PreCompilationAnalysisResult result)
        {
            var projectDir = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDir)) return;

            var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var csFile in csFiles)
            {
                var content = await File.ReadAllTextAsync(csFile);
                
                // Check for common issues
                if (!content.Contains("using System;") && content.Contains("Exception"))
                {
                    result.Recommendations.Add($"File {Path.GetFileName(csFile)} might need 'using System;'");
                }

                // Check for nullable reference warnings
                if (content.Contains("?") && !content.Contains("#nullable"))
                {
                    result.Recommendations.Add($"File {Path.GetFileName(csFile)} might benefit from nullable reference type annotations");
                }
            }
        }

        #endregion

        #region Auto-Fix Methods

        private async Task<List<AutoFix>> FixMissingUsingStatementsAsync(string projectPath)
        {
            var fixes = new List<AutoFix>();
            var projectDir = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDir)) return fixes;

            var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            var commonUsings = new Dictionary<string, string[]>
            {
                ["Exception"] = new[] { "using System;" },
                ["List<"] = new[] { "using System.Collections.Generic;" },
                ["Task"] = new[] { "using System.Threading.Tasks;" },
                ["HttpClient"] = new[] { "using System.Net.Http;" },
                ["JsonSerializer"] = new[] { "using System.Text.Json;" }
            };

            foreach (var csFile in csFiles)
            {
                var content = await File.ReadAllTextAsync(csFile);
                var modified = false;
                var newContent = content;

                foreach (var pattern in commonUsings)
                {
                    if (content.Contains(pattern.Key))
                    {
                        foreach (var usingStatement in pattern.Value)
                        {
                            if (!content.Contains(usingStatement))
                            {
                                newContent = AddUsingStatement(newContent, usingStatement);
                                modified = true;
                                
                                fixes.Add(new AutoFix
                                {
                                    Type = AutoFixType.AddUsingStatement,
                                    File = csFile,
                                    Description = $"Added {usingStatement}",
                                    AppliedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                }

                if (modified)
                {
                    await File.WriteAllTextAsync(csFile, newContent);
                }
            }

            return fixes;
        }

        private async Task<List<AutoFix>> FixPackageVersionConflictsAsync(string projectPath)
        {
            var fixes = new List<AutoFix>();

            // Implementation for package version conflict resolution
            // This would analyze and fix version conflicts in project files

            await Task.CompletedTask; // Placeholder for async operations
            return fixes;
        }

        private async Task<List<AutoFix>> FixProjectReferenceIssuesAsync(string projectPath)
        {
            var fixes = new List<AutoFix>();

            // Implementation for project reference issue resolution
            // This would fix broken or circular project references

            await Task.CompletedTask; // Placeholder for async operations
            return fixes;
        }

        private async Task<List<AutoFix>> FixNullableReferenceWarningsAsync(string projectPath)
        {
            var fixes = new List<AutoFix>();

            // Implementation for nullable reference warning fixes
            // This would add null checks and nullable annotations

            await Task.CompletedTask; // Placeholder for async operations
            return fixes;
        }

        private string AddUsingStatement(string content, string usingStatement)
        {
            var lines = content.Split('\n');
            var insertIndex = 0;

            // Find the best place to insert the using statement
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("using ") && line.EndsWith(";"))
                {
                    insertIndex = i + 1;
                }
                else if (line.StartsWith("namespace ") || line.StartsWith("public ") || line.StartsWith("internal "))
                {
                    break;
                }
            }

            var newLines = lines.ToList();
            newLines.Insert(insertIndex, usingStatement);
            return string.Join('\n', newLines);
        }

        #endregion
    }

    #region Resolution Strategies

    public interface IErrorResolutionStrategy
    {
        Task<CategoryResolutionResult> ResolveAsync(List<BuildError> errors);
    }

    public class PackageReferenceResolutionStrategy : IErrorResolutionStrategy
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;

        public PackageReferenceResolutionStrategy(string workspaceRoot, ILogger logger)
        {
            _workspaceRoot = workspaceRoot;
            _logger = logger;
        }

        public async Task<CategoryResolutionResult> ResolveAsync(List<BuildError> errors)
        {
            var result = new CategoryResolutionResult(ErrorCategory.PackageReference);

            foreach (var error in errors)
            {
                try
                {
                    // Attempt to resolve package reference issues
                    var resolved = await AttemptPackageResolutionAsync(error);
                    if (resolved)
                    {
                        result.ResolvedErrors.Add(error);
                        _logger.LogInfo($"Resolved package reference error: {error.Message}");
                    }
                    else
                    {
                        result.UnresolvedErrors.Add(error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to resolve package reference error: {error.Message}", ex);
                    result.UnresolvedErrors.Add(error);
                }
            }

            return result;
        }

        private async Task<bool> AttemptPackageResolutionAsync(BuildError error)
        {
            // Implementation for package resolution
            await Task.CompletedTask; // Placeholder for async operations
            return false; // Placeholder return
        }
    }

    public class ProjectReferenceResolutionStrategy : IErrorResolutionStrategy
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;

        public ProjectReferenceResolutionStrategy(string workspaceRoot, ILogger logger)
        {
            _workspaceRoot = workspaceRoot;
            _logger = logger;
        }

        public async Task<CategoryResolutionResult> ResolveAsync(List<BuildError> errors)
        {
            var result = new CategoryResolutionResult(ErrorCategory.ProjectReference);

            foreach (var error in errors)
            {
                try
                {
                    var resolved = await AttemptProjectReferenceResolutionAsync(error);
                    if (resolved)
                    {
                        result.ResolvedErrors.Add(error);
                        _logger.LogInfo($"Resolved project reference error: {error.Message}");
                    }
                    else
                    {
                        result.UnresolvedErrors.Add(error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to resolve project reference error: {error.Message}", ex);
                    result.UnresolvedErrors.Add(error);
                }
            }

            return result;
        }

        private async Task<bool> AttemptProjectReferenceResolutionAsync(BuildError error)
        {
            // Implementation for project reference resolution
            await Task.CompletedTask; // Placeholder for async operations
            return false; // Placeholder return
        }
    }

    public class CompilationErrorResolutionStrategy : IErrorResolutionStrategy
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;

        public CompilationErrorResolutionStrategy(string workspaceRoot, ILogger logger)
        {
            _workspaceRoot = workspaceRoot;
            _logger = logger;
        }

        public async Task<CategoryResolutionResult> ResolveAsync(List<BuildError> errors)
        {
            var result = new CategoryResolutionResult(ErrorCategory.CompilationError);

            foreach (var error in errors)
            {
                try
                {
                    var resolved = await AttemptCompilationErrorResolutionAsync(error);
                    if (resolved)
                    {
                        result.ResolvedErrors.Add(error);
                        _logger.LogInfo($"Resolved compilation error: {error.Message}");
                    }
                    else
                    {
                        result.UnresolvedErrors.Add(error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to resolve compilation error: {error.Message}", ex);
                    result.UnresolvedErrors.Add(error);
                }
            }

            return result;
        }

        private async Task<bool> AttemptCompilationErrorResolutionAsync(BuildError error)
        {
            // Implementation for compilation error resolution
            await Task.CompletedTask; // Placeholder for async operations
            return false; // Placeholder return
        }
    }

    public class FrameworkCompatibilityResolutionStrategy : IErrorResolutionStrategy
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;

        public FrameworkCompatibilityResolutionStrategy(string workspaceRoot, ILogger logger)
        {
            _workspaceRoot = workspaceRoot;
            _logger = logger;
        }

        public async Task<CategoryResolutionResult> ResolveAsync(List<BuildError> errors)
        {
            var result = new CategoryResolutionResult(ErrorCategory.FrameworkCompatibility);

            foreach (var error in errors)
            {
                try
                {
                    var resolved = await AttemptFrameworkCompatibilityResolutionAsync(error);
                    if (resolved)
                    {
                        result.ResolvedErrors.Add(error);
                        _logger.LogInfo($"Resolved framework compatibility error: {error.Message}");
                    }
                    else
                    {
                        result.UnresolvedErrors.Add(error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to resolve framework compatibility error: {error.Message}", ex);
                    result.UnresolvedErrors.Add(error);
                }
            }

            return result;
        }

        private async Task<bool> AttemptFrameworkCompatibilityResolutionAsync(BuildError error)
        {
            // Implementation for framework compatibility resolution
            await Task.CompletedTask; // Placeholder for async operations
            return false; // Placeholder return
        }
    }

    public class ConfigurationErrorResolutionStrategy : IErrorResolutionStrategy
    {
        private readonly string _workspaceRoot;
        private readonly ILogger _logger;

        public ConfigurationErrorResolutionStrategy(string workspaceRoot, ILogger logger)
        {
            _workspaceRoot = workspaceRoot;
            _logger = logger;
        }

        public async Task<CategoryResolutionResult> ResolveAsync(List<BuildError> errors)
        {
            var result = new CategoryResolutionResult(ErrorCategory.ConfigurationError);

            foreach (var error in errors)
            {
                try
                {
                    var resolved = await AttemptConfigurationErrorResolutionAsync(error);
                    if (resolved)
                    {
                        result.ResolvedErrors.Add(error);
                        _logger.LogInfo($"Resolved configuration error: {error.Message}");
                    }
                    else
                    {
                        result.UnresolvedErrors.Add(error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to resolve configuration error: {error.Message}", ex);
                    result.UnresolvedErrors.Add(error);
                }
            }

            return result;
        }

        private async Task<bool> AttemptConfigurationErrorResolutionAsync(BuildError error)
        {
            // Implementation for configuration error resolution
            await Task.CompletedTask; // Placeholder for async operations
            return false; // Placeholder return
        }
    }

    #endregion

    #region Result Classes

    public class CompilationResolutionResult
    {
        public List<BuildError> ResolvedErrors { get; } = new();
        public List<BuildError> UnresolvedErrors { get; } = new();
        public Dictionary<ErrorCategory, CategoryResolutionResult> CategoryResults { get; } = new();
        public TimeSpan Duration { get; set; }
        public double SuccessRate { get; set; }
        public Exception? Exception { get; set; }

        public void AddCategoryResult(ErrorCategory category, CategoryResolutionResult result)
        {
            CategoryResults[category] = result;
        }
    }

    public class CategoryResolutionResult
    {
        public ErrorCategory Category { get; }
        public List<BuildError> ResolvedErrors { get; } = new();
        public List<BuildError> UnresolvedErrors { get; } = new();
        public List<string> ResolutionActions { get; } = new();

        public CategoryResolutionResult(ErrorCategory category)
        {
            Category = category;
        }
    }

    public class PreCompilationAnalysisResult
    {
        public string ProjectPath { get; }
        public List<string> Issues { get; } = new();
        public List<string> Recommendations { get; } = new();
        public bool AnalysisCompleted { get; set; }
        public Exception? Exception { get; set; }

        public PreCompilationAnalysisResult(string projectPath)
        {
            ProjectPath = projectPath;
        }
    }

    public class AutoFixResult
    {
        public string ProjectPath { get; }
        public List<AutoFix> AppliedFixes { get; } = new();
        public bool Success { get; set; }
        public Exception? Exception { get; set; }

        public AutoFixResult(string projectPath)
        {
            ProjectPath = projectPath;
        }
    }

    public class AutoFix
    {
        public AutoFixType Type { get; set; }
        public string File { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
    }

    public enum AutoFixType
    {
        AddUsingStatement,
        FixPackageVersion,
        FixProjectReference,
        AddNullCheck,
        FixNullableAnnotation
    }

    #endregion
}