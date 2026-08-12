using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Forms;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Views
{
    public class SettingsView : UserControl
    {
        private readonly ISettingsService _settingsService;
        private ComboBox _cboTheme, _cboAccent, _cboLanguage, _cboInterval;
        private CheckBox _chkAutoRefresh;
        private TextBox _txtExportPath;
        private bool _isLoading = false;

        public SettingsView(ISettingsService settingsService)
        {
            _settingsService = settingsService; InitializeComponent(); LoadSettings(); ApplyTheme(); ThemeManager.ThemeChanged += ApplyTheme;
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill; this.Padding = new Padding(40);
            var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };

            var lblTitle = new Label { Text = "Application Settings", Font = new Font("Segoe UI", 24F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 30) };
            mainPanel.Controls.Add(lblTitle);

            mainPanel.Controls.Add(CreateSectionHeader("Appearance & Theming"));
            _cboTheme = CreateComboBox(new[] { "Dark", "Light" }); _cboTheme.SelectedIndexChanged += SaveThemeSettings;
            mainPanel.Controls.Add(CreateSettingRow("Application Theme:", _cboTheme));
            _cboAccent = CreateComboBox(new[] { "Sky Blue", "Emerald Green", "Royal Purple", "Ruby Red", "Amber Orange" }); _cboAccent.SelectedIndexChanged += SaveThemeSettings;
            mainPanel.Controls.Add(CreateSettingRow("Accent Color:", _cboAccent));

            mainPanel.Controls.Add(CreateSectionHeader("Application Behavior"));
            _cboLanguage = CreateComboBox(new[] { "English", "Spanish", "French", "German" }); _cboLanguage.SelectedIndexChanged += (s, e) => SaveStandardSettings();
            mainPanel.Controls.Add(CreateSettingRow("Language:", _cboLanguage));
            _cboInterval = CreateComboBox(new[] { "5 Seconds", "10 Seconds", "30 Seconds", "60 Seconds" }); _cboInterval.SelectedIndexChanged += (s, e) => SaveStandardSettings();
            mainPanel.Controls.Add(CreateSettingRow("Refresh Interval:", _cboInterval));

            _chkAutoRefresh = new CheckBox { Text = " Enable Auto-Refresh", Font = new Font("Segoe UI", 12F), AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(10, 10, 0, 0) };
            _chkAutoRefresh.CheckedChanged += (s, e) => SaveStandardSettings();
            mainPanel.Controls.Add(_chkAutoRefresh);

            mainPanel.Controls.Add(CreateSectionHeader("Data & Export Paths"));

            _txtExportPath = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12F), Margin = new Padding(0, 5, 10, 0) };
            _txtExportPath.TextChanged += (s, e) => SaveStandardSettings();
            var btnBrowse = new IconButton { Text = " Browse", IconChar = IconChar.FolderOpen, IconSize = 18, AutoSize = true, MinimumSize = new Size(120, 35), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBrowse.Click += (s, e) => { using (var fbd = new FolderBrowserDialog()) { if (fbd.ShowDialog() == DialogResult.OK) _txtExportPath.Text = fbd.SelectedPath; } };

            // BULLETPROOF PATH ROW
            var pathTlp = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 0, 0, 15), Width = 1000 };
            pathTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            pathTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pathTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var lblPath = new Label { Text = "PDF Export Folder:", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F), TextAlign = ContentAlignment.MiddleLeft };
            pathTlp.Controls.Add(lblPath, 0, 0); pathTlp.Controls.Add(_txtExportPath, 1, 0); pathTlp.Controls.Add(btnBrowse, 2, 0);
            mainPanel.Controls.Add(pathTlp);

            mainPanel.Controls.Add(CreateSectionHeader("Database Management"));

            var btnBackup = CreateActionButton("Backup Database", IconChar.Download, () => { using (var sfd = new SaveFileDialog { Filter = "SQLite|*.db", FileName = $"Backup.db" }) { if (sfd.ShowDialog() == DialogResult.OK) { _settingsService.BackupDatabase(sfd.FileName); ToastNotification.ShowSuccess("Backed up."); } } });
            var btnRestore = CreateActionButton("Restore Database", IconChar.Upload, () => { using (var ofd = new OpenFileDialog { Filter = "SQLite|*.db" }) { if (ofd.ShowDialog() == DialogResult.OK && MessageBox.Show("Overwrite?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { _settingsService.RestoreDatabase(ofd.FileName); ToastNotification.ShowSuccess("Restored!"); } } });
            var btnReset = CreateActionButton("Reset Defaults", IconChar.UndoAlt, () => { if (MessageBox.Show("Reset?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { _settingsService.ResetSettingsToDefault(); LoadSettings(); } });

            var dbPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            dbPanel.Controls.AddRange(new Control[] { btnBackup, btnRestore, btnReset });
            mainPanel.Controls.Add(dbPanel);

            this.Controls.Add(mainPanel);
        }

        private Label CreateSectionHeader(string text) => new Label { Text = text.ToUpper(), Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 40, 0, 15) };
        private ComboBox CreateComboBox(string[] items) { var cbo = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12F), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 0, 0) }; cbo.Items.AddRange(items); return cbo; }

        // BULLETPROOF SETTINGS ROW
        private TableLayoutPanel CreateSettingRow(string labelText, Control inputControl)
        {
            var tlp = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 15), Width = 800 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var lbl = new Label { Text = labelText, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 12F), TextAlign = ContentAlignment.MiddleLeft };
            tlp.Controls.Add(lbl, 0, 0);
            tlp.Controls.Add(inputControl, 1, 0);
            return tlp;
        }

        private IconButton CreateActionButton(string text, IconChar icon, Action onClick)
        {
            var btn = new IconButton { Text = $"  {text}", IconChar = icon, IconSize = 22, AutoSize = true, MinimumSize = new Size(220, 50), Margin = new Padding(0, 0, 20, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 11F), TextImageRelation = TextImageRelation.ImageBeforeText };
            btn.FlatAppearance.BorderSize = 1; btn.Click += (s, e) => onClick(); return btn;
        }

        private void LoadSettings() { _isLoading = true; _cboTheme.SelectedItem = _settingsService.Theme; _cboAccent.SelectedItem = _settingsService.AccentColor; _cboLanguage.SelectedItem = _settingsService.Language; _chkAutoRefresh.Checked = _settingsService.AutoRefresh; _txtExportPath.Text = _settingsService.ExportFolder; _cboInterval.SelectedItem = _settingsService.RefreshInterval == 5000 ? "5 Seconds" : _settingsService.RefreshInterval == 30000 ? "30 Seconds" : _settingsService.RefreshInterval == 60000 ? "60 Seconds" : "10 Seconds"; SaveThemeSettings(null, null); _isLoading = false; }
        private void SaveThemeSettings(object sender, EventArgs e) { if (_isLoading || _cboTheme.SelectedItem == null || _cboAccent.SelectedItem == null) return; _settingsService.Theme = _cboTheme.SelectedItem.ToString(); _settingsService.AccentColor = _cboAccent.SelectedItem.ToString(); ThemeManager.SetTheme(_settingsService.Theme == "Light" ? (ITheme)new LightTheme() : new DarkTheme(), _settingsService.AccentColor); }
        private void SaveStandardSettings() { if (_isLoading) return; _settingsService.Language = _cboLanguage.SelectedItem?.ToString() ?? "English"; _settingsService.AutoRefresh = _chkAutoRefresh.Checked; _settingsService.ExportFolder = _txtExportPath.Text; string i = _cboInterval.SelectedItem?.ToString() ?? "10 Seconds"; _settingsService.RefreshInterval = i.StartsWith("5") ? 5000 : i.StartsWith("30") ? 30000 : i.StartsWith("60") ? 60000 : 10000; }
        private void ApplyTheme() { this.BackColor = ThemeManager.CurrentTheme.Palette.Background; foreach (Control pnl in this.Controls[0].Controls) { if (pnl is Label h) h.ForeColor = ThemeManager.CurrentTheme.Palette.Primary; else if (pnl is TableLayoutPanel row) { foreach (Control c in row.Controls) { if (c is Label l) l.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; else if (c is ComboBox cb) { cb.BackColor = ThemeManager.CurrentTheme.Palette.Surface; cb.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; } else if (c is TextBox t) { t.BackColor = ThemeManager.CurrentTheme.Palette.Surface; t.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; } else if (c is IconButton btnRow) { btnRow.BackColor = ThemeManager.CurrentTheme.Palette.Surface; btnRow.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; btnRow.IconColor = ThemeManager.CurrentTheme.Palette.Primary; } } } else if (pnl is CheckBox chkOuter) { chkOuter.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; } else if (pnl is FlowLayoutPanel dbPanel) { foreach (IconButton btn in dbPanel.Controls) { btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface; btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary; btn.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.Palette.Border; } } } }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}