using System;
using RemoteITToolkit.Core.DTOs;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Services
{
    public class SystemInfoService : ISystemInfoService
    {
        public SystemInfoDTO GetCurrentSystemInfo()
        {
            return new SystemInfoDTO
            {
                CpuUsage = "14%",
                RamUsage = "8.2 GB / 16.0 GB",
                DiskSpace = "120 GB Free",
                WindowsVersion = "Win 11 Pro",
                LocalIp = "192.168.1.150",
                LastScan = DateTime.Now
            };
        }
    }
}