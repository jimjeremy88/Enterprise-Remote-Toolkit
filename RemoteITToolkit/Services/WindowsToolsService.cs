using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Management;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Services
{
    public class WindowsToolsService : IWindowsToolsService
    {
        private readonly IExtendedLogger _logger;

        public WindowsToolsService(IExtendedLogger logger)
        {
            _logger = logger;
        }

        public bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void Launch(string processName, string args = "")
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    Arguments = args,
                    UseShellExecute = true
                };

                if (!IsAdministrator())
                {
                    startInfo.Verb = "runas";
                }

                Process.Start(startInfo);
                _logger.LogActivity($"Launched Windows Tool: {processName}");
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                throw new Exception("Elevation request was cancelled by the user.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to launch {processName}", ex);
                throw new Exception($"Could not launch {processName}.");
            }
        }

        public void OpenTaskManager() => Launch("taskmgr");
        public void OpenServices() => Launch("services.msc");
        public void OpenEventViewer() => Launch("eventvwr.msc");
        public void OpenDeviceManager() => Launch("devmgmt.msc");
        public void OpenComputerManagement() => Launch("compmgmt.msc");
        public void OpenControlPanel() => Launch("control");
        public void OpenPowerShell() => Launch("powershell");
        public void OpenCommandPrompt() => Launch("cmd");
        public void OpenRegistryEditor() => Launch("regedit");
        public void OpenDiskManagement() => Launch("diskmgmt.msc");
        public void OpenSystemInformation() => Launch("msinfo32");
        public void OpenPerformanceMonitor() => Launch("perfmon.msc");
        public void OpenResourceMonitor() => Launch("resmon");
        public void OpenGroupPolicyEditor() => Launch("gpedit.msc");
        public void OpenWindowsUpdate() => Launch("ms-settings:windowsupdate");
        public void OpenWindowsDefender() => Launch("windowsdefender:");
        public void OpenWindowsTerminal() => Launch("wt");
        public void OpenDiskCleanup() => Launch("cleanmgr.exe");

        public async Task<bool> CreateRestorePointAsync(string description)
        {
            if (!IsAdministrator()) throw new Exception("Creating a restore point requires Administrator privileges.");
            return await Task.Run(() =>
            {
                try
                {
                    var scope = new ManagementScope(@"\\localhost\root\default");
                    var wmiClass = new ManagementClass(scope, new ManagementPath("SystemRestore"), null);
                    var inParams = wmiClass.GetMethodParameters("CreateRestorePoint");
                    inParams["Description"] = description;
                    inParams["RestorePointType"] = 12; // MODIFY_SETTINGS
                    inParams["EventType"] = 100; // BEGIN_SYSTEM_CHANGE
                    wmiClass.InvokeMethod("CreateRestorePoint", inParams, null);
                    _logger.LogSecurity($"System Restore Point created: {description}");
                    return true;
                }
                catch (Exception ex) { _logger.LogError("Restore point failed", ex); return false; }
            });
        }
    }
}