using System.Collections.Generic;

namespace RemoteITToolkit.Core.DTOs
{
    public class NetworkDiagnosticDTO
    {
        public string Hostname { get; set; } = "Unknown";
        public string LocalIp { get; set; } = "Unknown";
        public string PublicIp { get; set; } = "Unknown";
        public string MacAddress { get; set; } = "Unknown";
        public string ActiveAdapter { get; set; } = "Unknown";
        public bool IsInternetConnected { get; set; }
        public List<string> NetworkAdapters { get; set; } = new List<string>();
    }

    public class PingResultDTO { public string Address { get; set; } public long RoundtripTime { get; set; } public string Status { get; set; } }
    public class PortCheckResultDTO { public int Port { get; set; } public bool IsOpen { get; set; } public string Message { get; set; } }
    public class TracerouteHopDTO { public int Hop { get; set; } public string IpAddress { get; set; } public long Time { get; set; } }
}