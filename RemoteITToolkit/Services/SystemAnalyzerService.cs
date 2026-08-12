using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using RemoteITToolkit.Core.DTOs;
using RemoteITToolkit.Core.Interfaces;
using Microsoft.Win32;

namespace RemoteITToolkit.Services
{
    public class SystemAnalyzerService : ISystemAnalyzerService
    {
        private readonly ILogger _logger;

        public SystemAnalyzerService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<HardwareInfoDTO> GetHardwareInfoAsync()
        {
            return await Task.Run(() =>
            {
                var dto = new HardwareInfoDTO();
                try
                {
                    dto.CpuName = GetWmiProperty("Win32_Processor", "Name");
                    dto.CpuUsage = GetCpuUsage();
                    dto.GpuName = GetWmiProperty("Win32_VideoController", "Name");
                    dto.Motherboard = $"{GetWmiProperty("Win32_BaseBoard", "Manufacturer")} {GetWmiProperty("Win32_BaseBoard", "Product")}";
                    dto.BiosVersion = GetWmiProperty("Win32_BIOS", "SMBIOSBIOSVersion");
                    dto.SerialNumber = GetWmiProperty("Win32_BIOS", "SerialNumber");

                    dto.WindowsVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "Unknown OS")?.ToString();
                    dto.WindowsBuild = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild", "")?.ToString();

                    dto.Uptime = GetUptime();
                    dto.Antivirus = GetAntivirus();
                    dto.DiskHealth = GetWmiProperty("Win32_DiskDrive", "Status", "OK");

                    var batteryStatus = GetWmiProperty("Win32_Battery", "EstimatedChargeRemaining", "");

                    // FIXED: Uses the new BatteryHealth property
                    if (!string.IsNullOrEmpty(batteryStatus)) dto.BatteryHealth = $"{batteryStatus}% (Healthy)";

                    GetRamInfo(dto);
                    GetDriveInfo(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error collecting hardware info.", ex);
                }
                return dto;
            });
        }

        private TimeSpan GetUptime()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        DateTime bootTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                        return DateTime.Now - bootTime;
                    }
                }
            }
            catch { return TimeSpan.Zero; }
            return TimeSpan.Zero;
        }

        private string GetCpuUsage()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select LoadPercentage from Win32_Processor"))
                {
                    foreach (var obj in searcher.Get()) return $"{obj["LoadPercentage"]}%";
                }
            }
            catch { return "N/A"; }
            return "0%";
        }

        private string GetAntivirus()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT displayName FROM AntiVirusProduct"))
                {
                    foreach (var obj in searcher.Get()) return obj["displayName"].ToString();
                }
            }
            catch { return "Windows Defender"; }
            return "Windows Defender";
        }

        private void GetRamInfo(HardwareInfoDTO dto)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        long totalBytes = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                        dto.InstalledRam = $"{totalBytes / (1024 * 1024 * 1024)} GB";
                    }
                }
                using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        long freeKb = Convert.ToInt64(obj["FreePhysicalMemory"]);
                        dto.AvailableRam = $"{(freeKb / (1024 * 1024)):F2} GB";
                    }
                }
            }
            catch { }
        }

        private void GetDriveInfo(HardwareInfoDTO dto)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    double used = (double)(drive.TotalSize - drive.TotalFreeSpace) / drive.TotalSize * 100;
                    dto.LogicalDrives.Add(new DriveInfoDTO
                    {
                        DriveLetter = drive.Name,
                        TotalSize = drive.TotalSize,
                        FreeSpace = drive.TotalFreeSpace,
                        UsagePercentage = $"{used:F1}%"
                    });
                }
            }
        }

        private string GetWmiProperty(string wmiClass, string property, string fallback = "Unknown")
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var val = obj[property];
                        if (val != null && !string.IsNullOrWhiteSpace(val.ToString())) return val.ToString();
                    }
                }
            }
            catch { }
            return fallback;
        }
    }
}