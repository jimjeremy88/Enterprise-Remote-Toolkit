using System.Threading.Tasks;
using RemoteITToolkit.Core.DTOs;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface ISystemAnalyzerService
    {
        Task<HardwareInfoDTO> GetHardwareInfoAsync();
    }
}