using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Services
{
    public class RemoteSupportService : IRemoteSupportService
    {
        private readonly IExtendedLogger _logger;
        private readonly List<string> _clipboardHistory = new List<string>();
        private Timer _clipboardTimer;
        private string _lastClipboardText = "";

        public RemoteSupportService(IExtendedLogger logger)
        {
            _logger = logger;
        }

        private void LaunchProcess(string filename, string args = "")
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = filename, Arguments = args, UseShellExecute = true });
                _logger.LogActivity($"Launched Remote Tool: {filename}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to launch remote tool: {filename}", ex);
                throw new Exception($"Could not find or launch {Path.GetFileNameWithoutExtension(filename)}. Please ensure it is installed.");
            }
        }

        public void LaunchRDP() => LaunchProcess("mstsc.exe");
        public void LaunchQuickAssist() => LaunchProcess("quickassist.exe");
        public void LaunchRemoteAssistance() => LaunchProcess("msra.exe");

        public void LaunchAnyDesk()
        {
            string path1 = @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe";
            string path2 = Environment.ExpandEnvironmentVariables(@"%userprofile%\AppData\Local\AnyDesk\AnyDesk.exe");
            if (File.Exists(path1)) LaunchProcess(path1);
            else if (File.Exists(path2)) LaunchProcess(path2);
            else throw new Exception("AnyDesk is not installed on this system.");
        }

        public void LaunchTeamViewer()
        {
            string path1 = @"C:\Program Files\TeamViewer\TeamViewer.exe";
            string path2 = @"C:\Program Files (x86)\TeamViewer\TeamViewer.exe";
            if (File.Exists(path1)) LaunchProcess(path1);
            else if (File.Exists(path2)) LaunchProcess(path2);
            else throw new Exception("TeamViewer is not installed on this system.");
        }

        public void LaunchRustDesk()
        {
            string path1 = @"C:\Program Files\RustDesk\rustdesk.exe";
            if (File.Exists(path1)) LaunchProcess(path1);
            else throw new Exception("RustDesk is not installed on this system.");
        }

        public string GetSupportSummaryText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== REMOTE SUPPORT INFO ===");
            sb.AppendLine($"Hostname: {Environment.MachineName}");
            sb.AppendLine($"Username: {Environment.UserName}");
            sb.AppendLine($"OS Version: {Environment.OSVersion.VersionString}");
            string localIp = "Unknown";
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) { localIp = ip.ToString(); break; }
            }
            sb.AppendLine($"Local IP: {localIp}");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            return sb.ToString();
        }

        public void StartClipboardMonitor()
        {
            _clipboardTimer = new Timer { Interval = 2000 };
            _clipboardTimer.Tick += (s, e) =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string currentText = Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(currentText) && currentText != _lastClipboardText)
                        {
                            _lastClipboardText = currentText;
                            if (!_clipboardHistory.Contains(currentText))
                            {
                                _clipboardHistory.Insert(0, currentText);
                                if (_clipboardHistory.Count > 15) _clipboardHistory.RemoveAt(15);
                            }
                        }
                    }
                }
                catch { }
            };
            _clipboardTimer.Start();
        }

        public IEnumerable<string> GetClipboardHistory() => _clipboardHistory;

        public async Task<string> CaptureScreenshotAsync(string exportFolder)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);
                    string path = Path.Combine(exportFolder, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    var bounds = Screen.PrimaryScreen.Bounds;
                    using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    _logger.LogActivity($"Screenshot saved to {path}");
                    return path;
                }
                catch (Exception ex) { _logger.LogError("Screenshot failed", ex); throw; }
            });
        }
    }
}