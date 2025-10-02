using Inventory.Shared.Enums;
using System.Collections.Generic;
using System;

namespace Inventory.Shared.DTOs
{
    // AuditLogResponse is defined in AuditLogDto.cs
    // AuditLogDto is defined in AuditLogDto.cs

    /// <summary>
    /// DTO for audit statistics
    /// </summary>
    public class AuditStatisticsDto
    {
        public int TotalLogs { get; set; }
        public int SuccessfulLogs { get; set; }
        public int FailedLogs { get; set; }
        public Dictionary<string, int> LogsByAction { get; set; } = new();
        public Dictionary<string, int> LogsByEntity { get; set; } = new();
        public Dictionary<string, int> LogsBySeverity { get; set; } = new();
        public Dictionary<string, int> LogsByUser { get; set; } = new();
        public double AverageResponseTime { get; set; }
        public Dictionary<string, int> TopErrors { get; set; } = new();
    }

    /// <summary>
    /// DTO for cleanup result
    /// </summary>
    public class CleanupResultDto
    {
        public int DeletedCount { get; set; }
        public int DaysToKeep { get; set; }
        public DateTime CleanupDate { get; set; }
    }
}
