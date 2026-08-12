using System;

namespace RemoteITToolkit.Core.DTOs
{
    public class SystemInfoDTO
    {
        public string CpuUsage { get; set; }
        public string RamUsage { get; set; }
        public string DiskSpace { get; set; }
        public string WindowsVersion { get; set; }
        public string LocalIp { get; set; }
        public DateTime LastScan { get; set; }
    }
}