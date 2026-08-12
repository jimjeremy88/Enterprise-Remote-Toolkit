using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Theme;
using RemoteITToolkit.Presentation.Views;

namespace RemoteITToolkit.Presentation.Forms
{
    public class MainForm : Form
    {
        private Panel _sidebarPanel;
        private Panel _headerPanel;
        private Panel _contentPanel;
        private Panel _statusBar;
        private Label _lblTitle;
        private Label _lblStatusText;
        private Controls.LoadingAnimation _statusSpinner;

        // Views
        private readonly DashboardView _dashboardView;
        private readonly WindowsToolsView _windowsToolsView;
        private readonly NetworkDiagnosticView _networkView;
        private readonly RemoteSupportView _remoteSupportView;
        private readonly InstalledSoftwareView _softwareView;
        private readonly WindowsServicesView _servicesView;
        private readonly EventLogView _eventLogView;
        private readonly ReportGeneratorView _reportView;
        private readonly LogsView _logsView;
        private readonly SettingsView _settingsView;
        private readonly SystemInternalsView _internalsView;

        public MainForm(ISystemAnalyzerService sysAnalyzer, INetworkToolsService netTools, IWindowsToolsService winTools,
                        IRemoteSupportService remoteSupport, ISystemQueryService queryService, IReportGeneratorService reportService,
                        ISettingsService settingsService, IExtendedLogger logger)
        {
            _dashboardView = new DashboardView(sysAnalyzer, netTools);
            _dashboardView.OnStatusChanged += UpdateStatus;

            _windowsToolsView = new WindowsToolsView(winTools);
            _networkView = new NetworkDiagnosticView(netTools);
            _remoteSupportView = new RemoteSupportView(remoteSupport);
            _softwareView = new InstalledSoftwareView(queryService);
            _servicesView = new WindowsServicesView(queryService, winTools);
            _eventLogView = new EventLogView(queryService);
            _reportView = new ReportGeneratorView(reportService);
            _logsView = new LogsView(logger);
            _settingsView = new SettingsView(settingsService);
            _internalsView = new SystemInternalsView(queryService);

            InitializeComponent(winTools, remoteSupport, settingsService);

            ThemeManager.ApplyTheme(this);
            ApplyCustomSidebarTheming();
            ThemeManager.ThemeChanged += ApplyThemeWrapper;

            LoadView(_dashboardView, "System Dashboard", IconChar.ChartPie);
            ToastNotification.ShowInfo("System Boot Initialized.");
        }

        private void InitializeComponent(IWindowsToolsService winTools, IRemoteSupportService remoteSupport, ISettingsService settingsService)
        {
            this.Text = "Enterprise Remote Toolkit";
            this.Size = new Size(1600, 950);
            this.MinimumSize = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScaleMode = AutoScaleMode.Dpi;

            // --- SIDEBAR ---
            _sidebarPanel = new Panel { Dock = DockStyle.Left, Width = 300 };

            var logoPanel = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(25, 30, 0, 0) };
            var logoIcon = new IconPictureBox { IconChar = IconChar.Terminal, MinimumSize = new Size(1, 1), IconColor = Color.White, Size = new Size(36, 36), Dock = DockStyle.Left };
            var lblLogo = new Label { Text = " RemoteToolkit", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            logoPanel.Controls.Add(lblLogo);
            logoPanel.Controls.Add(logoIcon);
            _sidebarPanel.Controls.Add(logoPanel);

            _sidebarPanel.Controls.Add(CreateNavButton("Settings", IconChar.Cog, (s, e) => LoadView(_settingsView, "Application Settings", IconChar.Cog)));
            _sidebarPanel.Controls.Add(CreateNavButton("Audit Logs", IconChar.ClipboardCheck, (s, e) => LoadView(_logsView, "Enterprise Audit Logs", IconChar.ClipboardCheck)));
            _sidebarPanel.Controls.Add(CreateNavButton("Generate Report", IconChar.FilePdf, (s, e) => LoadView(_reportView, "IT Audit Report Generator", IconChar.FilePdf)));
            _sidebarPanel.Controls.Add(CreateNavButton("Event Logs", IconChar.ListAlt, (s, e) => LoadView(_eventLogView, "System Event Logs", IconChar.ListAlt)));
            _sidebarPanel.Controls.Add(CreateNavButton("System Internals", IconChar.Microchip, (s, e) => LoadView(_internalsView, "Startup, Tasks & Updates", IconChar.Microchip)));
            _sidebarPanel.Controls.Add(CreateNavButton("Services Monitor", IconChar.Cogs, (s, e) => LoadView(_servicesView, "Windows Services", IconChar.Cogs)));
            _sidebarPanel.Controls.Add(CreateNavButton("Software Manager", IconChar.BoxOpen, (s, e) => LoadView(_softwareView, "Installed Software", IconChar.BoxOpen)));
            _sidebarPanel.Controls.Add(CreateNavButton("Remote Support", IconChar.Headset, (s, e) => LoadView(_remoteSupportView, "Remote Support Hub", IconChar.Headset)));
            _sidebarPanel.Controls.Add(CreateNavButton("Network Tools", IconChar.NetworkWired, (s, e) => LoadView(_networkView, "Network Diagnostics", IconChar.NetworkWired)));
            _sidebarPanel.Controls.Add(CreateNavButton("Windows Tools", IconChar.Wrench, (s, e) => LoadView(_windowsToolsView, "Administrative Tools", IconChar.Wrench)));
            _sidebarPanel.Controls.Add(CreateNavButton("Dashboard", IconChar.ChartPie, (s, e) => LoadView(_dashboardView, "System Dashboard", IconChar.ChartPie)));

            // --- STATUS BAR ---
            _statusBar = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            _statusSpinner = new Controls.LoadingAnimation { Size = new Size(20, 20), SpinnerColor = Color.White, Location = new Point(20, 10) };
            _statusSpinner.Start();

            _lblStatusText = new Label { Text = "Ready", Font = new Font("Segoe UI Semibold", 11F), AutoSize = true, Location = new Point(50, 10) };

            _statusBar.Controls.Add(_statusSpinner);
            _statusBar.Controls.Add(_lblStatusText);

            _statusBar.Resize += (s, e) =>
            {
                _statusSpinner.Top = (_statusBar.Height - _statusSpinner.Height) / 2;
                _lblStatusText.Top = (_statusBar.Height - _lblStatusText.Height) / 2;
                _lblStatusText.Left = _statusSpinner.Right + 10;
            };

            // --- HEADER PANEL ---
            _headerPanel = new Panel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(40, 35, 40, 0) };

            // FIXED: Added UseMnemonic = false so it stops hiding the '&' symbol!
            _lblTitle = new Label { Text = "Dashboard", UseMnemonic = false, Font = new Font("Segoe UI", 24F, FontStyle.Bold), AutoSize = true, Location = new Point(30, 35), Padding = new Padding(0, 0, 0, 10) };

            var rightControlsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 0)
            };

            var btnSnap = new IconButton { Text = "", IconChar = IconChar.Camera, IconSize = 24, MinimumSize = new Size(1, 1), Width = 50, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(5, 0, 0, 0) };
            btnSnap.FlatAppearance.BorderSize = 0;
            btnSnap.Click += async (s, e) =>
            {
                try { await remoteSupport.CaptureScreenshotAsync(settingsService.ExportFolder); ToastNotification.ShowSuccess("Screenshot saved."); }
                catch { ToastNotification.Show("Screenshot failed.", IconChar.Times, ThemeManager.CurrentTheme.Palette.Error); }
            };

            var btnRestore = new IconButton { Text = "", IconChar = IconChar.History, IconSize = 24, MinimumSize = new Size(1, 1), Width = 50, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(5, 0, 5, 0) };
            btnRestore.FlatAppearance.BorderSize = 0;
            btnRestore.Click += async (s, e) =>
            {
                if (MessageBox.Show("Create System Restore Point?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ToastNotification.ShowInfo("Creating Restore Point... Please wait.");
                    bool ok = await winTools.CreateRestorePointAsync("RemoteToolkit Auto-Restore");
                    if (ok) ToastNotification.ShowSuccess("Restore Point Created!");
                    else ToastNotification.Show("Failed. Run as Administrator to create Restore Points.", IconChar.Lock, ThemeManager.CurrentTheme.Palette.Error);
                }
            };

            var txtSearch = new TextBox { Width = 350, Font = new Font("Segoe UI", 14F), Margin = new Padding(10, 5, 10, 0) };
            txtSearch.Text = "Search features...";
            txtSearch.GotFocus += (s, e) => { if (txtSearch.Text.StartsWith("Search")) txtSearch.Text = ""; };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    string term = txtSearch.Text.ToLower();
                    if (term.Contains("task") || term.Contains("manager")) winTools.OpenTaskManager();
                    else if (term.Contains("clean")) winTools.OpenDiskCleanup();
                    else if (term.Contains("ping") || term.Contains("trace")) LoadView(_networkView, "Network Diagnostics", IconChar.NetworkWired);
                    else if (term.Contains("service")) LoadView(_servicesView, "Windows Services", IconChar.Cogs);
                    else if (term.Contains("software") || term.Contains("app")) LoadView(_softwareView, "Installed Software", IconChar.BoxOpen);
                    else ToastNotification.Show("No match. Try navigating via the sidebar.", IconChar.Search, ThemeManager.CurrentTheme.Palette.Warning);
                    txtSearch.Text = "";
                }
            };

            rightControlsFlow.Controls.Add(btnSnap);
            rightControlsFlow.Controls.Add(btnRestore);
            rightControlsFlow.Controls.Add(txtSearch);

            _headerPanel.Controls.Add(rightControlsFlow);
            _headerPanel.Controls.Add(_lblTitle);

            _contentPanel = new Panel { Dock = DockStyle.Fill };
            this.Controls.Add(_contentPanel);
            this.Controls.Add(_headerPanel);
            this.Controls.Add(_statusBar);
            this.Controls.Add(_sidebarPanel);
        }

        private IconButton CreateNavButton(string text, IconChar icon, EventHandler onClick)
        {
            var btn = new IconButton
            {
                Text = $"   {text}",
                IconChar = icon,
                IconSize = 28,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Dock = DockStyle.Top,
                Height = 65,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F),
                Padding = new Padding(25, 0, 0, 0),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void LoadView(UserControl view, string title, IconChar icon)
        {
            _contentPanel.Controls.Clear();
            _contentPanel.Controls.Add(view);
            _lblTitle.Text = title;
            ApplyThemeWrapper();
        }

        public void UpdateStatus(string message)
        {
            if (_lblStatusText.InvokeRequired) _lblStatusText.Invoke(new Action(() => _lblStatusText.Text = message));
            else _lblStatusText.Text = message;
        }

        private void ApplyThemeWrapper()
        {
            ThemeManager.ApplyTheme(this);
            ApplyCustomSidebarTheming();
        }

        private void ApplyCustomSidebarTheming()
        {
            _sidebarPanel.BackColor = ThemeManager.CurrentTheme.Palette.SidebarBackground;
            _statusBar.BackColor = ThemeManager.CurrentTheme.Palette.Primary;
            _lblStatusText.ForeColor = Color.White;

            foreach (Control c in _sidebarPanel.Controls)
            {
                if (c is IconButton btn)
                {
                    btn.BackColor = ThemeManager.CurrentTheme.Palette.SidebarBackground;
                    btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
                    btn.IconColor = ThemeManager.CurrentTheme.Palette.TextSecondary;
                    btn.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentTheme.Palette.SidebarActive;
                }
            }

            _headerPanel.BackColor = ThemeManager.CurrentTheme.Palette.Background;

            foreach (Control c in _headerPanel.Controls)
            {
                if (c is FlowLayoutPanel flp) { foreach (Control child in flp.Controls) { if (child is TextBox t) { t.BackColor = ThemeManager.CurrentTheme.Palette.Surface; t.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; } if (child is IconButton b) { b.IconColor = ThemeManager.CurrentTheme.Palette.Primary; b.BackColor = ThemeManager.CurrentTheme.Palette.Background; } } }
            }
        }

        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyThemeWrapper; base.Dispose(disposing); }
    }
}