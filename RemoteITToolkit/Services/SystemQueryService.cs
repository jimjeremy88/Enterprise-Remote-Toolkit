using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using RemoteITToolkit.Core.DTOs;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Services
{
    public class SystemQueryService : ISystemQueryService
    {
        private readonly IExtendedLogger _logger;

        public SystemQueryService(IExtendedLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<InstalledProgramDTO>> GetInstalledProgramsAsync(string searchTerm = "")
        {
            return await Task.Run(() =>
            {
                var programs = new Dictionary<string, InstalledProgramDTO>(StringComparer.OrdinalIgnoreCase);
                string[] registryKeys = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                try
                {
                    foreach (var path in registryKeys)
                    {
                        RegistryKey rootKey = path == registryKeys[2] ? Registry.CurrentUser : Registry.LocalMachine;
                        using (RegistryKey key = rootKey.OpenSubKey(path))
                        {
                            if (key != null)
                            {
                                foreach (string subkeyName in key.GetSubKeyNames())
                                {
                                    using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                                    {
                                        string name = subkey?.GetValue("DisplayName")?.ToString();
                                        if (!string.IsNullOrWhiteSpace(name) && !name.StartsWith("KB") && !programs.ContainsKey(name))
                                        {
                                            programs.Add(name, new InstalledProgramDTO
                                            {
                                                Name = name,
                                                Version = subkey?.GetValue("DisplayVersion")?.ToString() ?? "-",
                                                Publisher = subkey?.GetValue("Publisher")?.ToString() ?? "-",
                                                InstallDate = FormatInstallDate(subkey?.GetValue("InstallDate")?.ToString())
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { _logger.LogError("Failed to get installed programs.", ex); }

                var results = programs.Values.AsEnumerable();
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    results = results.Where(p => p.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 || p.Publisher.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                return results.OrderBy(p => p.Name).ToList();
            });
        }

        private string FormatInstallDate(string date)
        {
            if (string.IsNullOrWhiteSpace(date) || date.Length != 8) return "-";
            try { return $"{date.Substring(0, 4)}-{date.Substring(4, 2)}-{date.Substring(6, 2)}"; }
            catch { return date; }
        }

        public async Task<IEnumerable<WindowsServiceDTO>> GetWindowsServicesAsync(string searchTerm = "")
        {
            return await Task.Run(() =>
            {
                var services = new List<WindowsServiceDTO>();
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Name, DisplayName, State, StartMode FROM Win32_Service"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            services.Add(new WindowsServiceDTO
                            {
                                Name = obj["Name"]?.ToString(),
                                DisplayName = obj["DisplayName"]?.ToString(),
                                Status = obj["State"]?.ToString(),
                                StartMode = obj["StartMode"]?.ToString()
                            });
                        }
                    }
                }
                catch (Exception ex) { _logger.LogError("Failed to get windows services.", ex); }
                return string.IsNullOrEmpty(searchTerm) ? services : services.Where(s => s.DisplayName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
            });
        }

        public async Task<IEnumerable<SystemEventLogDTO>> GetRecentEventLogsAsync(string logName = "System", int maxRecords = 100)
        {
            return await Task.Run(() =>
            {
                var logs = new List<SystemEventLogDTO>();
                try
                {
                    using (var eventLog = new EventLog(logName))
                    {
                        int count = 0;
                        for (int i = eventLog.Entries.Count - 1; i >= 0 && count < maxRecords; i--)
                        {
                            var entry = eventLog.Entries[i];
                            logs.Add(new SystemEventLogDTO
                            {
                                LogName = logName,
                                Source = entry.Source,
                                EventId = entry.InstanceId & 0x3FFFFFFF,
                                EntryType = entry.EntryType.ToString(),
                                Message = entry.Message,
                                TimeGenerated = entry.TimeGenerated
                            });
                            count++;
                        }
                    }
                }
                catch (System.Security.SecurityException)
                {
                    _logger.LogWarning($"Security Exception reading {logName} log. Try running as Administrator.");
                    logs.Add(new SystemEventLogDTO { LogName = logName, EntryType = "Error", Message = "Access Denied. Please Run as Administrator.", TimeGenerated = DateTime.Now, Source = "RemoteToolkit" });
                }
                catch (Exception ex) { _logger.LogError($"Failed to get event logs for {logName}.", ex); }
                return logs;
            });
        }

        // --- FIXED: Uses SC.EXE instead of missing references ---
        private async Task<bool> RunScCommand(string action, string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sc.exe",
                            Arguments = $"{action} \"{serviceName}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }
                    };
                    proc.Start();
                    proc.WaitForExit();
                    return proc.ExitCode == 0 || proc.ExitCode == 1056; // 0 is success, 1056 means already running
                }
                catch (Exception ex) { _logger.LogError($"SC command failed: {action} {serviceName}", ex); return false; }
            });
        }

        public async Task<bool> StartServiceAsync(string serviceName) => await RunScCommand("start", serviceName);
        public async Task<bool> StopServiceAsync(string serviceName) => await RunScCommand("stop", serviceName);

        public async Task<bool> RestartServiceAsync(string serviceName)
        {
            await RunScCommand("stop", serviceName);
            await Task.Delay(1500);
            return await RunScCommand("start", serviceName);
        }

        public async Task<IEnumerable<StartupProgramDTO>> GetStartupProgramsAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<StartupProgramDTO>();
                try
                {
                    string[] keys = { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run" };
                    foreach (var path in keys)
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(path))
                        {
                            if (key != null)
                            {
                                foreach (var name in key.GetValueNames())
                                    list.Add(new StartupProgramDTO { Name = name, Command = key.GetValue(name)?.ToString(), Location = "HKLM" });
                            }
                        }
                        using (var key = Registry.CurrentUser.OpenSubKey(path))
                        {
                            if (key != null)
                            {
                                foreach (var name in key.GetValueNames())
                                    list.Add(new StartupProgramDTO { Name = name, Command = key.GetValue(name)?.ToString(), Location = "HKCU" });
                            }
                        }
                    }
                }
                catch (Exception ex) { _logger.LogError("Failed to get startup programs", ex); }
                return list;
            });
        }

        public async Task<IEnumerable<ScheduledTaskDTO>> GetScheduledTasksAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<ScheduledTaskDTO>();
                try
                {
                    var proc = new Process { StartInfo = new ProcessStartInfo { FileName = "schtasks", Arguments = "/query /fo CSV /nh", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true } };
                    proc.Start();
                    var lines = proc.StandardOutput.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                        if (parts.Length >= 3)
                            list.Add(new ScheduledTaskDTO { TaskName = parts[0].Trim('"'), NextRunTime = parts[1].Trim('"'), Status = parts[2].Trim('"') });
                    }
                }
                catch (Exception ex) { _logger.LogError("Failed to get scheduled tasks", ex); }
                return list;
            });
        }

        public async Task<string> GetWindowsUpdateStatusAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Type t = Type.GetTypeFromProgID("Microsoft.Update.AutoUpdate");
                    if (t == null) return "Windows Update Service not available.";
                    dynamic au = Activator.CreateInstance(t);
                    dynamic results = au.Results;
                    DateTime lastSearch = results.LastSuccessfulSearchDate;
                    DateTime lastInstall = results.LastSuccessfulInstallationDate;
                    return $"Last Checked: {lastSearch.ToLocalTime():g}\nLast Installed: {lastInstall.ToLocalTime():g}";
                }
                catch { return "Requires Administrator privileges or WUA is disabled."; }
            });
        }
    }
}