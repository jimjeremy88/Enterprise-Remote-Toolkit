using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Controls;
using RemoteITToolkit.Presentation.Helpers;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Views
{
    public class DashboardView : UserControl
    {
        private readonly ISystemAnalyzerService _sysAnalyzer;
        private readonly INetworkToolsService _netTools;

        private FlowLayoutPanel _layoutPanel;
        private Panel _toolbarPanel;
        private IconButton _btnRefresh;
        private CheckBox _chkAutoRefresh;
        private Timer _refreshTimer;

        private CardControl _cpuCard, _ramCard, _gpuCard, _moboCard, _diskCard;
        private CardControl _osCard, _avCard, _uptimeCard, _batteryCard;
        private CardControl _networkCard;

        // Quick Access Controls
        private Panel _pnlQuickWrapper;
        private Panel _quickAccentBar;
        private TableLayoutPanel _pnlQuick;

        public event Action<string> OnStatusChanged;

        public DashboardView(ISystemAnalyzerService sysAnalyzer, INetworkToolsService netTools)
        {
            _sysAnalyzer = sysAnalyzer;
            _netTools = netTools;
            InitializeComponent();
            this.BackColor = Color.Transparent;
            ThemeManager.ThemeChanged += ApplyTheme;
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(30);

            _toolbarPanel = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(0, 10, 0, 10) };

            _btnRefresh = new IconButton { Text = "  Refresh Data", IconChar = IconChar.Sync, IconSize = 20, Width = 180, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 11F) };
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.Click += async (s, e) => { try { await LoadDataAsync(); } catch { } };

            _chkAutoRefresh = new CheckBox { Text = "Auto Refresh (10s)", Dock = DockStyle.Left, Width = 200, Padding = new Padding(20, 0, 0, 0), Font = new Font("Segoe UI", 11F), Checked = true, Cursor = Cursors.Hand };
            _refreshTimer = new Timer { Interval = 10000, Enabled = true };
            _refreshTimer.Tick += async (s, e) => { try { await LoadDataAsync(); } catch { } };
            _chkAutoRefresh.CheckedChanged += (s, e) => _refreshTimer.Enabled = _chkAutoRefresh.Checked;

            _toolbarPanel.Controls.Add(_chkAutoRefresh);
            _toolbarPanel.Controls.Add(_btnRefresh);

            _layoutPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, Padding = new Padding(0, 20, 0, 0) };

            _cpuCard = new CardControl("Processor (CPU)", "Loading...", IconChar.Microchip, true);
            _ramCard = new CardControl("Memory (RAM)", "Loading...", IconChar.Memory, true);
            _gpuCard = new CardControl("Graphics (GPU)", "Loading...", IconChar.Desktop);
            _moboCard = new CardControl("Motherboard & BIOS", "Loading...", IconChar.Microblog);
            _osCard = new CardControl("Windows Details", "Loading...", IconChar.Windows);
            _uptimeCard = new CardControl("System Uptime", "Loading...", IconChar.Clock);
            _avCard = new CardControl("Antivirus Security", "Loading...", IconChar.ShieldAlt);
            _batteryCard = new CardControl("Battery Health", "Loading...", IconChar.BatteryFull);
            _diskCard = new CardControl("System Drive", "Loading...", IconChar.Hdd, true);
            _networkCard = new CardControl("Network & IPs", "Loading...", IconChar.NetworkWired);

            // FIXED: Bulletproof TableLayout for Quick Access (Matches the other Cards exactly)
            _pnlQuickWrapper = new Panel { Size = new Size(380, 180), Margin = new Padding(15), Padding = new Padding(1) };
            _quickAccentBar = new Panel { Dock = DockStyle.Left, Width = 5 };

            _pnlQuick = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            _pnlQuick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _pnlQuick.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            _pnlQuick.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _pnlQuick.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var lblQ = new Label { Text = "QUICK ACCESS", Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 5, 0, 0) };

            var btnDisk = new IconButton { Text = "  Disk Cleanup", IconChar = IconChar.Broom, IconSize = 28, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, ImageAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 12F), Cursor = Cursors.Hand, Padding = new Padding(10, 0, 0, 0) };
            btnDisk.FlatAppearance.BorderSize = 0;
            btnDisk.Click += (s, e) => System.Diagnostics.Process.Start("cleanmgr.exe");

            var btnTerm = new IconButton { Text = "  PowerShell", IconChar = IconChar.Terminal, IconSize = 28, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, ImageAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 12F), Cursor = Cursors.Hand, Padding = new Padding(10, 0, 0, 0) };
            btnTerm.FlatAppearance.BorderSize = 0;
            btnTerm.Click += (s, e) => System.Diagnostics.Process.Start("powershell.exe");

            _pnlQuick.Controls.Add(lblQ, 0, 0);
            _pnlQuick.Controls.Add(btnDisk, 0, 1);
            _pnlQuick.Controls.Add(btnTerm, 0, 2);

            _pnlQuickWrapper.Controls.Add(_pnlQuick);
            _pnlQuickWrapper.Controls.Add(_quickAccentBar);

            // Add everything to the layout panel
            _layoutPanel.Controls.AddRange(new Control[] { _cpuCard, _ramCard, _diskCard, _gpuCard, _moboCard, _osCard, _networkCard, _avCard, _uptimeCard, _batteryCard, _pnlQuickWrapper });

            this.Controls.Add(_layoutPanel);
            this.Controls.Add(_toolbarPanel);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                this.SafeInvoke(() => { OnStatusChanged?.Invoke("Fetching telemetry..."); _btnRefresh.Enabled = false; });
                var hwTask = _sysAnalyzer.GetHardwareInfoAsync();
                var netTask = _netTools.GetNetworkInfoAsync();
                await Task.WhenAll(hwTask, netTask);

                var hw = hwTask.Result;
                var net = netTask.Result;

                this.SafeInvoke(() =>
                {
                    int cpuVal = 0; int.TryParse(hw.CpuUsage.Replace("%", ""), out cpuVal);
                    _cpuCard.UpdateValue($"{hw.CpuName}\nUsage: {hw.CpuUsage}", cpuVal);

                    long totalR = 0, availR = 0;
                    if (long.TryParse(hw.InstalledRam.Split(' ')[0], out totalR) && double.TryParse(hw.AvailableRam.Split(' ')[0], out double availD))
                    {
                        availR = (long)availD;
                        int ramPct = totalR > 0 ? (int)(((totalR - availR) / (double)totalR) * 100) : 0;
                        _ramCard.UpdateValue($"{hw.InstalledRam} Total\nAvail: {hw.AvailableRam}", ramPct);
                    }
                    else _ramCard.UpdateValue($"{hw.InstalledRam} Total\nAvail: {hw.AvailableRam}");

                    _gpuCard.UpdateValue(FormatLongString(hw.GpuName, 30));
                    _moboCard.UpdateValue($"{hw.Motherboard}\nBIOS: {hw.BiosVersion}");
                    _osCard.UpdateValue($"{hw.WindowsVersion}\nBuild: {hw.WindowsBuild}");
                    _uptimeCard.UpdateValue($"{hw.Uptime.Days}d, {hw.Uptime.Hours}h, {hw.Uptime.Minutes}m");
                    _avCard.UpdateValue(hw.Antivirus);
                    _batteryCard.UpdateValue(hw.BatteryHealth);

                    var sysDrive = hw.LogicalDrives.FirstOrDefault(d => d.DriveLetter.StartsWith("C"));
                    if (sysDrive != null)
                    {
                        string freeGb = (sysDrive.FreeSpace / (1024 * 1024 * 1024)).ToString();
                        int diskPct = 0; int.TryParse(sysDrive.UsagePercentage.Replace("%", ""), out diskPct);
                        _diskCard.UpdateValue($"{sysDrive.UsagePercentage} Used\n{freeGb} GB Free", diskPct);
                    }

                    _networkCard.UpdateValue($"Local: {net.LocalIp}\nMAC: {net.MacAddress}");
                    OnStatusChanged?.Invoke("System telemetry up to date.");
                });
            }
            catch (Exception) { this.SafeInvoke(() => { OnStatusChanged?.Invoke("Error fetching data."); }); }
            finally { this.SafeInvoke(() => _btnRefresh.Enabled = true); }
        }

        private string FormatLongString(string input, int maxLength = 25)
        {
            if (string.IsNullOrEmpty(input)) return "Unknown";
            return input.Length > maxLength ? input.Substring(0, maxLength - 3) + "..." : input;
        }

        private void ApplyTheme()
        {
            _btnRefresh.BackColor = ThemeManager.CurrentTheme.Palette.Primary;
            _btnRefresh.ForeColor = Color.White;
            _btnRefresh.IconColor = Color.White;
            _chkAutoRefresh.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;

            // Apply theme to the new Quick Access Card structure
            _pnlQuickWrapper.BackColor = ThemeManager.CurrentTheme.Palette.Border;
            _quickAccentBar.BackColor = ThemeManager.CurrentTheme.Palette.Primary;
            _pnlQuick.BackColor = ThemeManager.CurrentTheme.Palette.Surface;

            foreach (Control c in _pnlQuick.Controls)
            {
                if (c is IconButton btn)
                {
                    btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface;
                    btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
                    btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary;
                    btn.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentTheme.Palette.Background;
                }
                if (c is Label l) l.ForeColor = ThemeManager.CurrentTheme.Palette.TextSecondary;
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= ApplyTheme; _refreshTimer?.Dispose(); } base.Dispose(disposing); }
    }
}