using RemoteITToolkit.Core.DTOs;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface ISystemInfoService
    {
        SystemInfoDTO GetCurrentSystemInfo();
    }
}