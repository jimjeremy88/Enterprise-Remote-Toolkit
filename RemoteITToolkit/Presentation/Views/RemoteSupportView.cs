using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Forms;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Views
{
    public class RemoteSupportView : UserControl
    {
        private readonly IRemoteSupportService _remoteSupportService;
        private FlowLayoutPanel _topPanel;
        private FlowLayoutPanel _bottomPanel;
        private TextBox _txtSupportInfo;

        public RemoteSupportView(IRemoteSupportService remoteSupportService)
        {
            _remoteSupportService = remoteSupportService;
            InitializeComponent();
            ApplyTheme();
            ThemeManager.ThemeChanged += ApplyTheme;
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill; this.Padding = new Padding(30); this.BackColor = Color.Transparent;

            // FIXED: UseMnemonic = false stops WinForms from hiding the '&' symbol!
            var lblTitle = new Label { Text = "Remote Desktop & Assistance", UseMnemonic = false, Font = new Font("Segoe UI", 20F, FontStyle.Bold), Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };

            _topPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 20) };

            _topPanel.Controls.Add(CreateLauncherCard("Remote Desktop", "Connect to a PC (RDP)", IconChar.Desktop, () => _remoteSupportService.LaunchRDP()));
            _topPanel.Controls.Add(CreateLauncherCard("Quick Assist", "Provide remote help", IconChar.HandsHelping, () => _remoteSupportService.LaunchQuickAssist()));
            _topPanel.Controls.Add(CreateLauncherCard("Remote Assistance", "Legacy MSRA tool", IconChar.UserFriends, () => _remoteSupportService.LaunchRemoteAssistance()));
            _topPanel.Controls.Add(CreateLauncherCard("AnyDesk", "Third-party remote", IconChar.At, () => _remoteSupportService.LaunchAnyDesk()));
            _topPanel.Controls.Add(CreateLauncherCard("TeamViewer", "Third-party remote", IconChar.ArrowsAltH, () => _remoteSupportService.LaunchTeamViewer()));
            _topPanel.Controls.Add(CreateLauncherCard("RustDesk", "Open-source remote", IconChar.Connectdevelop, () => _remoteSupportService.LaunchRustDesk()));

            var lblInfoTitle = new Label { Text = "Support Identity Info", Font = new Font("Segoe UI", 18F, FontStyle.Bold), Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 20, 0, 15) };

            _bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            _bottomPanel.Controls.Add(CreateActionButton("Copy Computer Name", IconChar.Building, () => CopyText(Environment.MachineName)));
            _bottomPanel.Controls.Add(CreateActionButton("Copy Support Summary", IconChar.ClipboardList, () => CopyText(_remoteSupportService.GetSupportSummaryText())));

            _txtSupportInfo = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, Font = new Font("Consolas", 12F), Margin = new Padding(0, 20, 0, 0), Text = _remoteSupportService.GetSupportSummaryText() };

            this.Controls.Add(_txtSupportInfo);
            this.Controls.Add(_bottomPanel);
            this.Controls.Add(lblInfoTitle);
            this.Controls.Add(_topPanel);
            this.Controls.Add(lblTitle);
        }

        // FIXED: Expanded the Size to 400x160, turned AutoSize off on labels to force text wrapping
        private Panel CreateLauncherCard(string title, string subtitle, IconChar icon, Action onClick)
        {
            var tlp = new TableLayoutPanel { Size = new Size(400, 160), Margin = new Padding(0, 0, 20, 20), Cursor = Cursors.Hand, BorderStyle = BorderStyle.FixedSingle, ColumnCount = 2, RowCount = 2 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F)); // Left col for Icon
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Right col for Text
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 55F)); // Title needs a bit more room
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 45F)); // Subtitle

            var iconBox = new IconPictureBox { IconChar = icon, MinimumSize = new Size(1, 1), IconSize = 56, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Color.Transparent };

            // AutoSize = false forces the text to wrap instead of truncating
            var lblTitle = new Label { Text = title, Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI Semibold", 14F), Padding = new Padding(0, 0, 0, 5) };
            var lblSubtitle = new Label { Text = subtitle, Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.TopLeft, Font = new Font("Segoe UI", 11F) };

            tlp.Controls.Add(iconBox, 0, 0);
            tlp.SetRowSpan(iconBox, 2);
            tlp.Controls.Add(lblTitle, 1, 0);
            tlp.Controls.Add(lblSubtitle, 1, 1);

            tlp.Click += (s, e) => SafeClick(onClick);
            iconBox.Click += (s, e) => SafeClick(onClick);
            lblTitle.Click += (s, e) => SafeClick(onClick);
            lblSubtitle.Click += (s, e) => SafeClick(onClick);

            return tlp;
        }

        // FIXED: Realigned buttons to ensure icon and text sit perfectly side-by-side
        private IconButton CreateActionButton(string text, IconChar icon, Action onClick)
        {
            var btn = new IconButton
            {
                Text = $"  {text}",
                IconChar = icon,
                IconSize = 24,
                AutoSize = true,
                MinimumSize = new Size(280, 60),
                Margin = new Padding(0, 0, 20, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 12F),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 15, 0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += (s, e) => SafeClick(onClick);
            return btn;
        }

        private void SafeClick(Action action) { try { action(); } catch (Exception ex) { ToastNotification.Show(ex.Message, IconChar.ExclamationCircle, ThemeManager.CurrentTheme.Palette.Error); } }
        private void CopyText(string text) { Clipboard.SetText(text); ToastNotification.ShowSuccess("Copied to clipboard!"); }

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.Palette.Background;
            _txtSupportInfo.BackColor = ThemeManager.CurrentTheme.Palette.Surface;
            _txtSupportInfo.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;

            foreach (Control c in this.Controls) if (c is Label lbl) lbl.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;

            foreach (Control c in _topPanel.Controls) { if (c is TableLayoutPanel tlp) { tlp.BackColor = ThemeManager.CurrentTheme.Palette.Surface; ColorizePanel(tlp); } }
            foreach (Control c in _bottomPanel.Controls) { if (c is IconButton btn) { btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface; btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary; btn.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.Palette.Border; btn.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentTheme.Palette.Background; } }
        }

        private void ColorizePanel(Control p)
        {
            foreach (Control c in p.Controls)
            {
                if (c is Label l) l.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
                if (c is IconPictureBox i) i.IconColor = ThemeManager.CurrentTheme.Palette.Primary;
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}