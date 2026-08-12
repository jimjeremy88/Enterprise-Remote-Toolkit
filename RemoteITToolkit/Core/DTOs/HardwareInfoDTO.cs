using System;
using System.Collections.Generic;

namespace RemoteITToolkit.Core.DTOs
{
    public class HardwareInfoDTO
    {
        public string CpuName { get; set; } = "Unknown";
        public string CpuUsage { get; set; } = "0%";
        public string GpuName { get; set; } = "Unknown";
        public string Motherboard { get; set; } = "Unknown";
        public string BiosVersion { get; set; } = "Unknown";
        public string SerialNumber { get; set; } = "Unknown";
        public string WindowsVersion { get; set; } = "Unknown";
        public string WindowsBuild { get; set; } = "Unknown";
        public TimeSpan Uptime { get; set; }
        public string InstalledRam { get; set; } = "Unknown";
        public string AvailableRam { get; set; } = "Unknown";
        public string BatteryHealth { get; set; } = "No Battery";
        public string Antivirus { get; set; } = "Windows Defender";
        public string DiskHealth { get; set; } = "Unknown";
        public List<DriveInfoDTO> LogicalDrives { get; set; } = new List<DriveInfoDTO>();
    }

    public class DriveInfoDTO
    {
        public string DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
        public string DriveFormat { get; set; }
        public long TotalSize { get; set; }
        public long FreeSpace { get; set; }
        public string UsagePercentage { get; set; }
    }
}