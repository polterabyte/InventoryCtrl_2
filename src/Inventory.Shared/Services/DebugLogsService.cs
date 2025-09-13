using System.Collections.Concurrent;
using System.Text.Json;

namespace Inventory.Shared.Services;

public interface IDebugLogsService
{
    Task<List<LogEntry>> GetLogsAsync(int count = 100);
    Task<List<LogEntry>> GetLogsByLevelAsync(LogLevel level, int count = 100);
    Task<List<LogEntry>> GetLogsByTimeRangeAsync(DateTime startTime, DateTime endTime, int count = 100);
    Task<LogEntry?> GetLogByIdAsync(string id);
    Task ClearLogsAsync();
    IAsyncEnumerable<LogEntry> StreamLogsAsync(CancellationToken cancellationToken = default);
    Task AddLogAsync(LogEntry logEntry);
    event EventHandler<LogEntry>? LogAdded;
}

public class DebugLogsService : IDebugLogsService
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly int _maxLogs = 1000;
    private readonly string _logDirectory;

    public event EventHandler<LogEntry>? LogAdded;

    public DebugLogsService()
    {
        _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        EnsureLogDirectoryExists();
        LoadExistingLogs();
    }

    public async Task<List<LogEntry>> GetLogsAsync(int count = 100)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _logs.TakeLast(count).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<LogEntry>> GetLogsByLevelAsync(LogLevel level, int count = 100)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _logs
                .Where(log => log.Level == level)
                .TakeLast(count)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<LogEntry>> GetLogsByTimeRangeAsync(DateTime startTime, DateTime endTime, int count = 100)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _logs
                .Where(log => log.Timestamp >= startTime && log.Timestamp <= endTime)
                .TakeLast(count)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<LogEntry?> GetLogByIdAsync(string id)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _logs.FirstOrDefault(log => log.Id == id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearLogsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            while (_logs.TryDequeue(out _)) { }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async IAsyncEnumerable<LogEntry> StreamLogsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lastLogId = string.Empty;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var newLogs = _logs
                .Where(log => string.Compare(log.Id, lastLogId) > 0)
                .OrderBy(log => log.Timestamp)
                .ToList();

            foreach (var log in newLogs)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return log;
                lastLogId = log.Id;
            }

            await Task.Delay(1000, cancellationToken);
        }
    }

    public async Task AddLogAsync(LogEntry logEntry)
    {
        await _semaphore.WaitAsync();
        try
        {
            _logs.Enqueue(logEntry);
            
            // Remove old logs if we exceed the limit
            while (_logs.Count > _maxLogs)
            {
                _logs.TryDequeue(out _);
            }

            LogAdded?.Invoke(this, logEntry);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void EnsureLogDirectoryExists()
    {
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    private void LoadExistingLogs()
    {
        try
        {
            var logFiles = Directory.GetFiles(_logDirectory, "*.txt")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Take(5); // Load last 5 log files

            foreach (var logFile in logFiles)
            {
                var lines = File.ReadAllLines(logFile);
                foreach (var line in lines.TakeLast(200)) // Load last 200 lines from each file
                {
                    if (TryParseLogLine(line, out var logEntry))
                    {
                        _logs.Enqueue(logEntry);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail initialization
            Console.WriteLine($"Error loading existing logs: {ex.Message}");
        }
    }

    private bool TryParseLogLine(string line, out LogEntry logEntry)
    {
        logEntry = new LogEntry();
        
        try
        {
            // Parse log line format: [Timestamp] [Level] Message
            var parts = line.Split(']', 3);
            if (parts.Length >= 3)
            {
                var timestampPart = parts[0].TrimStart('[');
                var levelPart = parts[1].TrimStart('[').Trim();
                
                if (DateTime.TryParse(timestampPart, out var timestamp) &&
                    Enum.TryParse<LogLevel>(levelPart, true, out var level))
                {
                    logEntry = new LogEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = timestamp,
                        Level = level,
                        Message = parts[2].Trim(),
                        Source = "File"
                    };
                    return true;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        
        return false;
    }
}

public class LogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Exception { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}
