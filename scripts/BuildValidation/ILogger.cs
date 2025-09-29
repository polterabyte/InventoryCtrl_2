using System;

namespace InventoryCtrl.BuildValidation
{
    /// <summary>
    /// Logger interface for build validation system
    /// </summary>
    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? exception = null);
        void LogDebug(string message);
    }

    /// <summary>
    /// Console logger implementation for build validation
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly bool _enableDebug;

        public ConsoleLogger(bool enableDebug = false)
        {
            _enableDebug = enableDebug;
        }

        public void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            Console.ResetColor();
        }

        public void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            Console.ResetColor();
        }

        public void LogError(string message, Exception? exception = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception.Message}");
                if (_enableDebug)
                {
                    Console.WriteLine($"Stack Trace: {exception.StackTrace}");
                }
            }
            Console.ResetColor();
        }

        public void LogDebug(string message)
        {
            if (_enableDebug)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
                Console.ResetColor();
            }
        }
    }
}