using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RemoteITToolkit.Core.DTOs;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Services
{
    public class NetworkToolsService : INetworkToolsService
    {
        private readonly ILogger _logger;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly int[] _commonPorts = { 21, 22, 23, 25, 53, 80, 110, 135, 139, 143, 443, 3389 };

        public NetworkToolsService(ILogger logger) { _logger = logger; }

        public async Task<NetworkDiagnosticDTO> GetNetworkInfoAsync()
        {
            var dto = new NetworkDiagnosticDTO();
            try
            {
                dto.Hostname = Dns.GetHostName();
                dto.LocalIp = GetLocalIPAddress();
                dto.PublicIp = await GetPublicIpAsync();

                var activeNic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                if (activeNic != null)
                {
                    dto.ActiveAdapter = activeNic.Name;
                    dto.MacAddress = string.Join(":", activeNic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
                }
            }
            catch (Exception ex) { _logger.LogError("Error collecting network info", ex); }
            return dto;
        }

        public async Task<PingResultDTO> PingHostAsync(string host)
        {
            try { using (var ping = new Ping()) { var reply = await ping.SendPingAsync(host, 3000); return new PingResultDTO { Address = reply.Address?.ToString() ?? host, RoundtripTime = reply.RoundtripTime, Status = reply.Status.ToString() }; } }
            catch (Exception ex) { return new PingResultDTO { Address = host, Status = ex.InnerException?.Message ?? ex.Message }; }
        }

        public async Task<IEnumerable<TracerouteHopDTO>> TracerouteAsync(string host, int maxHops = 30)
        {
            var hops = new List<TracerouteHopDTO>();
            try
            {
                var ip = (await Dns.GetHostAddressesAsync(host))[0];
                using (var ping = new Ping())
                {
                    for (int ttl = 1; ttl <= maxHops; ttl++)
                    {
                        var options = new PingOptions(ttl, true);
                        byte[] buffer = new byte[32];
                        var reply = await ping.SendPingAsync(ip, 2000, buffer, options);
                        hops.Add(new TracerouteHopDTO { Hop = ttl, IpAddress = reply.Address?.ToString() ?? "*", Time = reply.RoundtripTime });
                        if (reply.Status == IPStatus.Success) break;
                    }
                }
            }
            catch { hops.Add(new TracerouteHopDTO { Hop = 1, IpAddress = "Traceroute Failed" }); }
            return hops;
        }

        public async Task<string> RunWhoisAsync(string host)
        {
            return await Task.Run(() =>
            {
                try { using (var client = new TcpClient()) { client.Connect("whois.verisign-grs.com", 43); client.ReceiveTimeout = 3000; using (var stream = client.GetStream()) using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true }) using (var reader = new StreamReader(stream, Encoding.ASCII)) { writer.WriteLine(host); return reader.ReadToEnd(); } } }
                catch (Exception ex) { return $"Whois lookup failed: {ex.Message}"; }
            });
        }

        public async Task<string> ScanCommonPortsAsync(string host)
        {
            var sb = new StringBuilder(); sb.AppendLine($"--- Scanning Common Ports for {host} ---");
            foreach (var port in _commonPorts) { var result = await CheckPortAsync(host, port); sb.AppendLine($"Port {port,-5} : {(result.IsOpen ? "OPEN" : "CLOSED")}"); }
            return sb.ToString();
        }

        public async Task<PortCheckResultDTO> CheckPortAsync(string host, int port)
        {
            using (var client = new TcpClient()) { try { var connectTask = client.ConnectAsync(host, port); if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask) { return new PortCheckResultDTO { Port = port, IsOpen = client.Connected, Message = "Success" }; } return new PortCheckResultDTO { Port = port, IsOpen = false, Message = "Timeout" }; } catch { return new PortCheckResultDTO { Port = port, IsOpen = false, Message = "Failed" }; } }
        }

        public async Task<string> ResolveDnsAsync(string host)
        {
            try { var ips = await Dns.GetHostAddressesAsync(host); return string.Join(Environment.NewLine, ips.Select(ip => ip.ToString())); }
            catch (Exception ex) { return $"DNS Resolution Failed: {ex.Message}"; }
        }

        public async Task<string> ExecuteIpConfigCommandAsync(string arguments)
        {
            return await Task.Run(() =>
            {
                try { var process = new Process { StartInfo = new ProcessStartInfo { FileName = "ipconfig", Arguments = arguments, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true } }; process.Start(); string result = process.StandardOutput.ReadToEnd(); process.WaitForExit(); return result; }
                catch (Exception ex) { return $"Command failed: {ex.Message}"; }
            });
        }

        public async Task<string> TestLatencyAsync(string host, int pings = 4)
        {
            var sb = new StringBuilder(); sb.AppendLine($"--- Latency Test to {host} ({pings} packets) ---");
            long totalTime = 0; int successful = 0;
            for (int i = 0; i < pings; i++) { var result = await PingHostAsync(host); if (result.Status == "Success") { sb.AppendLine($"Reply from {result.Address}: time={result.RoundtripTime}ms"); totalTime += result.RoundtripTime; successful++; } else { sb.AppendLine("Request timed out."); } await Task.Delay(500); }
            if (successful > 0) sb.AppendLine($"\nAverage Latency: {totalTime / successful}ms ({successful}/{pings} successful)");
            return sb.ToString();
        }

        public async Task<string> SimulateBandwidthAsync()
        {
            return await Task.Run(async () =>
            {
                try { var sw = Stopwatch.StartNew(); var data = await _httpClient.GetByteArrayAsync("https://speed.cloudflare.com/__down?bytes=1000000"); sw.Stop(); double mbps = (data.Length * 8.0 / 1000000.0) / (sw.ElapsedMilliseconds / 1000.0); return $"Simulated Bandwidth:\nSpeed: {mbps:F2} Mbps\nLatency: {sw.ElapsedMilliseconds} ms"; }
                catch { return "Bandwidth test failed."; }
            });
        }

        private string GetLocalIPAddress() { foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList) if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString(); return "127.0.0.1"; }
        private async Task<string> GetPublicIpAsync() { try { return (await _httpClient.GetStringAsync("https://api.ipify.org")).Trim(); } catch { return "Unavailable"; } }
    }
}