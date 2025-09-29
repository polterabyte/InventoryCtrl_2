using System;
using System.IO;
using System.Threading.Tasks;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Command-line interface for the build validation system
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== InventoryCtrl Build Validation System ===");
                Console.WriteLine("Implementing comprehensive error detection and resolution per design document");
                Console.WriteLine();

                var workspaceRoot = GetWorkspaceRoot(args);
                var orchestrator = new BuildValidationOrchestrator(workspaceRoot);

                if (args.Length == 0)
                {
                    return await RunInteractiveMode(orchestrator);
                }

                return await RunCommandMode(orchestrator, args);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static async Task<int> RunInteractiveMode(BuildValidationOrchestrator orchestrator)
        {
            Console.WriteLine("Interactive Mode - Select validation option:");
            Console.WriteLine("1. Complete Validation Suite");
            Console.WriteLine("2. Build Validation Only");
            Console.WriteLine("3. Docker Validation Only");
            Console.WriteLine("4. Environment Validation Only");
            Console.WriteLine("5. Testing Only");
            Console.WriteLine("6. Monitoring Setup Only");
            Console.WriteLine("7. System Health Check");
            Console.WriteLine("8. Exit");
            Console.Write("Select option (1-8): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    return await RunCompleteValidation(orchestrator);
                case "2":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Build);
                case "3":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Docker);
                case "4":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Environment);
                case "5":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Testing);
                case "6":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Monitoring);
                case "7":
                    return await RunHealthCheck(orchestrator);
                case "8":
                    Console.WriteLine("Exiting...");
                    return 0;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    return await RunInteractiveMode(orchestrator);
            }
        }

        private static async Task<int> RunCommandMode(BuildValidationOrchestrator orchestrator, string[] args)
        {
            var command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "validate":
                case "all":
                    return await RunCompleteValidation(orchestrator);
                
                case "build":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Build);
                
                case "docker":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Docker);
                
                case "environment":
                case "env":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Environment);
                
                case "test":
                case "tests":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Testing);
                
                case "monitoring":
                case "monitor":
                    return await RunTargetedValidation(orchestrator, ValidationTarget.Monitoring);
                
                case "health":
                    return await RunHealthCheck(orchestrator);
                
                case "diagnose":
                    return await RunDiagnosis(orchestrator, args);
                
                case "help":
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;
                
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    PrintHelp();
                    return 1;
            }
        }

        private static async Task<int> RunCompleteValidation(BuildValidationOrchestrator orchestrator)
        {
            Console.WriteLine("Starting complete validation suite...");
            Console.WriteLine();

            var result = await orchestrator.ExecuteCompleteValidationAsync();
            
            PrintValidationResult(result);
            
            return result.OverallStatus == ValidationStatus.Passed ? 0 : 1;
        }

        private static async Task<int> RunTargetedValidation(BuildValidationOrchestrator orchestrator, ValidationTarget target)
        {
            Console.WriteLine($"Starting {target} validation...");
            Console.WriteLine();

            var result = await orchestrator.ExecuteTargetedValidationAsync(target);
            
            PrintValidationResult(result);
            
            return result.OverallStatus == ValidationStatus.Passed ? 0 : 1;
        }

        private static async Task<int> RunHealthCheck(BuildValidationOrchestrator orchestrator)
        {
            Console.WriteLine("Performing system health check...");
            Console.WriteLine();

            // Create monitoring system for health check
            var workspaceRoot = Environment.CurrentDirectory;
            var logger = new ConsoleLogger(enableDebug: false);
            var monitoringSystem = new MonitoringSystem(workspaceRoot, logger);

            var healthStatus = await monitoringSystem.CheckSystemHealthAsync();
            
            PrintHealthStatus(healthStatus);
            
            return healthStatus.OverallHealth == HealthStatus.Healthy ? 0 : 1;
        }

        private static async Task<int> RunDiagnosis(BuildValidationOrchestrator orchestrator, string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: diagnose <error-log-file>");
                return 1;
            }

            var logFile = args[1];
            if (!File.Exists(logFile))
            {
                Console.WriteLine($"Error log file not found: {logFile}");
                return 1;
            }

            Console.WriteLine($"Analyzing error log: {logFile}");
            Console.WriteLine();

            var errorLog = await File.ReadAllTextAsync(logFile);
            var result = await orchestrator.DiagnoseAndResolveErrorsAsync(errorLog);
            
            PrintDiagnosisResult(result);
            
            return result.Success ? 0 : 1;
        }

        private static void PrintValidationResult(OrchestrationResult result)
        {
            Console.WriteLine("=== VALIDATION RESULTS ===");
            Console.WriteLine($"Overall Status: {GetStatusDisplay(result.OverallStatus)}");
            Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F2} seconds");
            Console.WriteLine();

            if (result.BuildValidationResult != null)
            {
                Console.WriteLine($"Build Validation: {GetStatusDisplay(result.BuildValidationResult.OverallStatus)}");
                if (result.BuildValidationResult.OverallStatus != ValidationStatus.Passed)
                {
                    var errorCount = result.BuildValidationResult.GetAllErrors().Count();
                    Console.WriteLine($"  Errors found: {errorCount}");
                }
            }

            if (result.DockerValidationResult != null)
            {
                Console.WriteLine($"Docker Validation: {GetStatusDisplay(result.DockerValidationResult.OverallStatus)}");
                if (result.DockerValidationResult.Errors.Count > 0)
                {
                    Console.WriteLine($"  Docker errors: {result.DockerValidationResult.Errors.Count}");
                }
            }

            if (result.EnvironmentValidationResult != null)
            {
                Console.WriteLine($"Environment Validation: {GetStatusDisplay(result.EnvironmentValidationResult.OverallStatus)}");
                if (result.EnvironmentValidationResult.Errors.Count > 0)
                {
                    Console.WriteLine($"  Environment errors: {result.EnvironmentValidationResult.Errors.Count}");
                }
            }

            if (result.TestingResult != null)
            {
                Console.WriteLine($"Testing: {GetTestStatusDisplay(result.TestingResult.OverallStatus)}");
                var failedProjects = result.TestingResult.TestResults.Values
                    .SelectMany(r => r.FailedProjects).Count();
                if (failedProjects > 0)
                {
                    Console.WriteLine($"  Failed test projects: {failedProjects}");
                }
            }

            if (result.MonitoringResult != null)
            {
                Console.WriteLine($"Monitoring Setup: {GetMonitoringStatusDisplay(result.MonitoringResult.OverallStatus)}");
            }

            if (result.Exception != null)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception: {result.Exception.Message}");
                Console.ResetColor();
            }

            if (result.Report != null)
            {
                Console.WriteLine();
                Console.WriteLine("=== DETAILED REPORT ===");
                foreach (var section in result.Report.Sections)
                {
                    Console.WriteLine($"{section.Key}: {section.Value.Summary}");
                    if (section.Value.ErrorCount > 0)
                    {
                        Console.WriteLine($"  Errors: {section.Value.ErrorCount}");
                    }
                }
            }
        }

        private static void PrintHealthStatus(SystemHealthStatus healthStatus)
        {
            Console.WriteLine("=== SYSTEM HEALTH STATUS ===");
            Console.WriteLine($"Overall Health: {GetHealthStatusDisplay(healthStatus.OverallHealth)}");
            Console.WriteLine($"Check Time: {healthStatus.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            Console.WriteLine("Component Health:");
            foreach (var component in healthStatus.ComponentHealth)
            {
                var status = component.Value.IsHealthy ? "✓" : "✗";
                var color = component.Value.IsHealthy ? ConsoleColor.Green : ConsoleColor.Red;
                
                Console.ForegroundColor = color;
                Console.Write($"  {status} {component.Key}");
                Console.ResetColor();
                
                if (!component.Value.IsHealthy && !string.IsNullOrEmpty(component.Value.ErrorMessage))
                {
                    Console.Write($" - {component.Value.ErrorMessage}");
                }
                Console.WriteLine();
            }

            if (healthStatus.Exception != null)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Health check exception: {healthStatus.Exception.Message}");
                Console.ResetColor();
            }
        }

        private static void PrintDiagnosisResult(DiagnosisResult result)
        {
            Console.WriteLine("=== DIAGNOSIS RESULTS ===");
            Console.WriteLine($"Success: {(result.Success ? "Yes" : "No")}");
            
            if (result.ResolutionResult != null)
            {
                Console.WriteLine($"Resolution Rate: {result.ResolutionResult.SuccessRate:P2}");
                Console.WriteLine($"Errors Resolved: {result.ResolutionResult.ResolvedErrors.Count}");
                Console.WriteLine($"Errors Unresolved: {result.ResolutionResult.UnresolvedErrors.Count}");
            }

            if (!string.IsNullOrEmpty(result.DiagnosisReport))
            {
                Console.WriteLine();
                Console.WriteLine(result.DiagnosisReport);
            }

            if (result.Exception != null)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Diagnosis exception: {result.Exception.Message}");
                Console.ResetColor();
            }
        }

        private static string GetStatusDisplay(ValidationStatus status)
        {
            return status switch
            {
                ValidationStatus.Passed => "✓ PASSED",
                ValidationStatus.Failed => "✗ FAILED",
                ValidationStatus.Error => "⚠ ERROR",
                ValidationStatus.Skipped => "- SKIPPED",
                _ => "? UNKNOWN"
            };
        }

        private static string GetTestStatusDisplay(TestStatus status)
        {
            return status switch
            {
                TestStatus.Passed => "✓ PASSED",
                TestStatus.Failed => "✗ FAILED",
                TestStatus.Error => "⚠ ERROR",
                TestStatus.Skipped => "- SKIPPED",
                _ => "? UNKNOWN"
            };
        }

        private static string GetHealthStatusDisplay(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Healthy => "✓ HEALTHY",
                HealthStatus.Warning => "⚠ WARNING",
                HealthStatus.Critical => "✗ CRITICAL",
                _ => "? UNKNOWN"
            };
        }

        private static string GetMonitoringStatusDisplay(MonitoringStatus status)
        {
            return status switch
            {
                MonitoringStatus.Operational => "✓ OPERATIONAL",
                MonitoringStatus.Degraded => "⚠ DEGRADED",
                MonitoringStatus.Failed => "✗ FAILED",
                MonitoringStatus.Unknown => "? UNKNOWN",
                _ => "? UNKNOWN"
            };
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: BuildValidation [command] [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  validate, all       - Run complete validation suite");
            Console.WriteLine("  build              - Run build validation only");
            Console.WriteLine("  docker             - Run Docker validation only");
            Console.WriteLine("  environment, env   - Run environment validation only");
            Console.WriteLine("  test, tests        - Run testing validation only");
            Console.WriteLine("  monitoring, monitor- Set up monitoring only");
            Console.WriteLine("  health             - Perform system health check");
            Console.WriteLine("  diagnose <logfile> - Diagnose errors from log file");
            Console.WriteLine("  help, --help, -h   - Show this help message");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --workspace <path> - Specify workspace root directory");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  BuildValidation validate");
            Console.WriteLine("  BuildValidation docker --workspace /path/to/project");
            Console.WriteLine("  BuildValidation diagnose error.log");
            Console.WriteLine("  BuildValidation health");
        }

        private static string GetWorkspaceRoot(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--workspace")
                {
                    return args[i + 1];
                }
            }

            // Try to find workspace root automatically
            var currentDir = Environment.CurrentDirectory;
            var dir = new DirectoryInfo(currentDir);

            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Directory.Packages.props")) ||
                    File.Exists(Path.Combine(dir.FullName, "global.json")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return currentDir;
        }
    }
}