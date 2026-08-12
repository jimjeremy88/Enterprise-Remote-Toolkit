using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteITToolkit.Core.DTOs;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface INetworkToolsService
    {
        Task<NetworkDiagnosticDTO> GetNetworkInfoAsync();
        Task<PingResultDTO> PingHostAsync(string host);
        Task<PortCheckResultDTO> CheckPortAsync(string host, int port);
        Task<IEnumerable<TracerouteHopDTO>> TracerouteAsync(string host, int maxHops = 30);
        Task<string> ResolveDnsAsync(string host);

        Task<string> RunWhoisAsync(string host);
        Task<string> ExecuteIpConfigCommandAsync(string arguments);
        Task<string> TestLatencyAsync(string host, int pings = 4);
        Task<string> SimulateBandwidthAsync();
        Task<string> ScanCommonPortsAsync(string host);
    }
}