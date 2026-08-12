using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteITToolkit.Core.DTOs;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface ISystemQueryService
    {
        Task<IEnumerable<InstalledProgramDTO>> GetInstalledProgramsAsync(string searchTerm = "");
        Task<IEnumerable<WindowsServiceDTO>> GetWindowsServicesAsync(string searchTerm = "");
        Task<IEnumerable<SystemEventLogDTO>> GetRecentEventLogsAsync(string logName = "System", int maxRecords = 100);

        Task<bool> StartServiceAsync(string serviceName);
        Task<bool> StopServiceAsync(string serviceName);
        Task<bool> RestartServiceAsync(string serviceName);

        Task<IEnumerable<StartupProgramDTO>> GetStartupProgramsAsync();
        Task<IEnumerable<ScheduledTaskDTO>> GetScheduledTasksAsync();
        Task<string> GetWindowsUpdateStatusAsync();
    }
}