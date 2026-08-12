using System;

namespace RemoteITToolkit.Core.DTOs
{
    public class InstalledProgramDTO
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string InstallDate { get; set; }
    }

    public class WindowsServiceDTO
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
        public string StartMode { get; set; }
    }

    public class SystemEventLogDTO
    {
        public string LogName { get; set; }
        public string Source { get; set; }
        public long EventId { get; set; }
        public string EntryType { get; set; }
        public string Message { get; set; }
        public DateTime TimeGenerated { get; set; }
    }

    public class StartupProgramDTO
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string Location { get; set; }
    }

    public class ScheduledTaskDTO
    {
        public string TaskName { get; set; }
        public string NextRunTime { get; set; }
        public string Status { get; set; }
    }
}