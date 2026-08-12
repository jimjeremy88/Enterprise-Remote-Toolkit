using System.Threading.Tasks;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface IWindowsToolsService
    {
        bool IsAdministrator();

        void OpenTaskManager();
        void OpenServices();
        void OpenEventViewer();
        void OpenDeviceManager();
        void OpenComputerManagement();
        void OpenControlPanel();
        void OpenPowerShell();
        void OpenCommandPrompt();
        void OpenRegistryEditor();
        void OpenDiskManagement();
        void OpenSystemInformation();
        void OpenPerformanceMonitor();
        void OpenResourceMonitor();
        void OpenWindowsUpdate();
        void OpenWindowsDefender();
        void OpenGroupPolicyEditor();
        void OpenWindowsTerminal();
        void OpenDiskCleanup();

        Task<bool> CreateRestorePointAsync(string description);
    }
}