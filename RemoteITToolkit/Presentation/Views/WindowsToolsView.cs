using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Forms;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Views
{
    public class WindowsToolsView : UserControl
    {
        private readonly IWindowsToolsService _toolsService;
        private FlowLayoutPanel _statusBanner;
        private Label _lblAdminStatus;
        private IconPictureBox _iconAdminStatus;
        private FlowLayoutPanel _gridPanel;

        public WindowsToolsView(IWindowsToolsService toolsService)
        {
            _toolsService = toolsService; InitializeComponent(); ApplyTheme(); ThemeManager.ThemeChanged += ApplyTheme;
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill; this.Padding = new Padding(30); this.BackColor = Color.Transparent;

            _statusBanner = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(20, 15, 20, 15), WrapContents = false };

            _iconAdminStatus = new IconPictureBox { IconChar = IconChar.ShieldAlt, MinimumSize = new Size(32, 32), IconSize = 32, Size = new Size(32, 32), BackColor = Color.Transparent, Margin = new Padding(0, 0, 15, 0) };
            _lblAdminStatus = new Label { Font = new Font("Segoe UI Semibold", 14F), AutoSize = true, Padding = new Padding(0, 2, 0, 0) };

            if (_toolsService.IsAdministrator()) { _iconAdminStatus.IconChar = IconChar.ShieldAlt; _lblAdminStatus.Text = "Running as Administrator: Full Access Granted"; }
            else { _iconAdminStatus.IconChar = IconChar.ExclamationTriangle; _lblAdminStatus.Text = "Standard User: Tools will prompt for UAC Elevation"; }

            _statusBanner.Controls.Add(_iconAdminStatus); _statusBanner.Controls.Add(_lblAdminStatus);
            var spacer = new Panel { Dock = DockStyle.Top, Height = 30 };

            _gridPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0) };

            _gridPanel.Controls.Add(CreateTile("Task Manager", IconChar.Microchip, () => _toolsService.OpenTaskManager()));
            _gridPanel.Controls.Add(CreateTile("Services", IconChar.Cogs, () => _toolsService.OpenServices()));
            _gridPanel.Controls.Add(CreateTile("Event Viewer", IconChar.ListAlt, () => _toolsService.OpenEventViewer()));
            _gridPanel.Controls.Add(CreateTile("Device Manager", IconChar.Laptop, () => _toolsService.OpenDeviceManager()));
            _gridPanel.Controls.Add(CreateTile("Computer Mgmt", IconChar.Server, () => _toolsService.OpenComputerManagement()));
            _gridPanel.Controls.Add(CreateTile("Control Panel", IconChar.SlidersH, () => _toolsService.OpenControlPanel()));
            _gridPanel.Controls.Add(CreateTile("PowerShell", IconChar.Terminal, () => _toolsService.OpenPowerShell(), true));
            _gridPanel.Controls.Add(CreateTile("Command Prompt", IconChar.Terminal, () => _toolsService.OpenCommandPrompt(), true));
            _gridPanel.Controls.Add(CreateTile("Windows Terminal", IconChar.Code, () => _toolsService.OpenWindowsTerminal(), true));
            _gridPanel.Controls.Add(CreateTile("Registry Editor", IconChar.FolderOpen, () => _toolsService.OpenRegistryEditor()));
            _gridPanel.Controls.Add(CreateTile("Disk Mgmt", IconChar.Hdd, () => _toolsService.OpenDiskManagement()));
            _gridPanel.Controls.Add(CreateTile("Group Policy", IconChar.UsersCog, () => _toolsService.OpenGroupPolicyEditor()));
            _gridPanel.Controls.Add(CreateTile("System Info", IconChar.InfoCircle, () => _toolsService.OpenSystemInformation()));
            _gridPanel.Controls.Add(CreateTile("Perf Monitor", IconChar.ChartLine, () => _toolsService.OpenPerformanceMonitor()));
            _gridPanel.Controls.Add(CreateTile("Resource Monitor", IconChar.TachometerAlt, () => _toolsService.OpenResourceMonitor()));
            _gridPanel.Controls.Add(CreateTile("Windows Update", IconChar.SyncAlt, () => _toolsService.OpenWindowsUpdate()));
            _gridPanel.Controls.Add(CreateTile("Windows Defender", IconChar.ShieldVirus, () => _toolsService.OpenWindowsDefender()));

            this.Controls.Add(_gridPanel); this.Controls.Add(spacer); this.Controls.Add(_statusBanner);
        }

        private Panel CreateTile(string text, IconChar icon, Action onClick, bool isTerminal = false)
        {
            // FIXED: Widened the tiles to 240px to give the massive 200% font room to breathe
            var pnl = new TableLayoutPanel { Size = new Size(240, 160), Margin = new Padding(0, 0, 20, 20), Cursor = Cursors.Hand, Tag = isTerminal, BorderStyle = BorderStyle.FixedSingle, ColumnCount = 1, RowCount = 2 };
            pnl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // FIXED: Gave the text row an absolute height of 60px
            pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));

            var iconBox = new IconPictureBox { IconChar = icon, IconSize = 48, MinimumSize = new Size(48, 48), Size = new Size(48, 48), Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Color.Transparent };

            // FIXED: AutoSize is FALSE. Dock is FILL. This forces the text to WRAP to a new line instead of chopping off the sides!
            var lbl = new Label { Text = text, Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 12F), Padding = new Padding(5, 0, 5, 10) };

            pnl.Controls.Add(iconBox, 0, 0); pnl.Controls.Add(lbl, 0, 1);

            pnl.Click += (s, e) => SafeClick(onClick); iconBox.Click += (s, e) => SafeClick(onClick); lbl.Click += (s, e) => SafeClick(onClick);

            return pnl;
        }

        private void SafeClick(Action action) { try { action(); } catch (Exception ex) { ToastNotification.Show(ex.Message, IconChar.ExclamationCircle, ThemeManager.CurrentTheme.Palette.Error); } }

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.Palette.Background;
            _statusBanner.BackColor = _toolsService.IsAdministrator() ? ThemeManager.CurrentTheme.Palette.Success : ThemeManager.CurrentTheme.Palette.Warning;
            _lblAdminStatus.ForeColor = Color.White; _iconAdminStatus.IconColor = Color.White;

            foreach (Control c in _gridPanel.Controls)
            {
                if (c is TableLayoutPanel tlp)
                {
                    tlp.BackColor = ThemeManager.CurrentTheme.Palette.Surface;
                    foreach (Control child in tlp.Controls)
                    {
                        if (child is Label l) l.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
                        if (child is IconPictureBox ib) ib.IconColor = (bool)tlp.Tag ? ThemeManager.CurrentTheme.Palette.Primary : ThemeManager.CurrentTheme.Palette.TextPrimary;
                    }
                }
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}