using System;

namespace RemoteITToolkit.Core.DTOs
{
    public class LogEntryDTO
    {
        public string Category { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string Operator { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}