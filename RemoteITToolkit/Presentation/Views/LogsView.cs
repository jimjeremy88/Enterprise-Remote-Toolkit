using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.DTOs;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Forms;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Views
{
    public class LogsView : UserControl
    {
        private readonly IExtendedLogger _loggerService;
        private List<LogEntryDTO> _allLogs;
        private DataGridView _grid;
        private ComboBox _cboCategory;
        private TextBox _txtSearch;
        private FlowLayoutPanel _toolbar;
        private Label _lblCount;

        public LogsView(IExtendedLogger loggerService)
        {
            _loggerService = loggerService; _allLogs = new List<LogEntryDTO>();
            InitializeComponent(); ApplyTheme(); ThemeManager.ThemeChanged += ApplyTheme; LoadData();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill; this.Padding = new Padding(30);

            // FIXED: Using FlowLayoutPanel
            _toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };

            _cboCategory = new ComboBox { Width = 220, Font = new Font("Segoe UI", 14F), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 20, 0) };
            _cboCategory.Items.AddRange(new[] { "All Categories", "Application", "Error", "Security", "User Actions", "Network" });
            _cboCategory.SelectedIndex = 0;
            _cboCategory.SelectedIndexChanged += (s, e) => FilterData();

            _txtSearch = new TextBox { Width = 350, Font = new Font("Segoe UI", 14F), Text = "Search logs...", Margin = new Padding(0, 5, 20, 0) };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Search logs...") _txtSearch.Text = ""; };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) _txtSearch.Text = "Search logs..."; };
            _txtSearch.TextChanged += (s, e) => { if (_txtSearch.Text != "Search logs...") FilterData(); };

            var btnRefresh = CreateToolbarButton("Refresh", IconChar.Sync); btnRefresh.Click += (s, e) => LoadData();
            var btnExport = CreateToolbarButton("CSV", IconChar.FileCsv); btnExport.Click += (s, e) => ExportData();

            _lblCount = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), Margin = new Padding(20, 8, 0, 0) };

            _toolbar.Controls.AddRange(new Control[] { _cboCategory, _txtSearch, btnRefresh, btnExport, _lblCount });

            _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, EnableHeadersVisualStyles = false, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, RowTemplate = { Height = 50 }, ColumnHeadersHeight = 50 };
            _grid.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CreatedAt", HeaderText = "Date/Time", Width = 250 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Category", HeaderText = "Category", Width = 200 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LogLevel", HeaderText = "Level", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Operator", HeaderText = "User", Width = 200 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Message", HeaderText = "Message", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            _grid.CellFormatting += Grid_CellFormatting;

            this.Controls.Add(_grid);
            this.Controls.Add(_toolbar);
        }

        private IconButton CreateToolbarButton(string text, IconChar icon)
        {
            var btn = new IconButton
            {
                Text = $"  {text}",
                IconChar = icon,
                IconSize = 24,
                AutoSize = true,
                MinimumSize = new Size(130, 45),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 12F),
                Margin = new Padding(0, 0, 15, 0),
                Padding = new Padding(10, 5, 10, 5),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderSize = 0; return btn;
        }

        private void LoadData() { _allLogs = _loggerService.GetRecentLogs(1000).ToList(); FilterData(); }
        private void FilterData() { var filtered = _allLogs.AsEnumerable(); if (_cboCategory.SelectedIndex > 0) filtered = filtered.Where(l => l.Category == _cboCategory.SelectedItem.ToString()); string search = _txtSearch.Text.Trim(); if (!string.IsNullOrEmpty(search) && search != "Search logs...") filtered = filtered.Where(l => l.Message.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || l.Operator.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0); var result = filtered.ToList(); _grid.DataSource = result; _lblCount.Text = $"{result.Count} Logs"; }
        private void ExportData() { using (var sfd = new SaveFileDialog { Filter = "CSV Files|*.csv", FileName = $"SystemLogs_{DateTime.Now:yyyyMMdd}.csv" }) { if (sfd.ShowDialog() == DialogResult.OK) { var sb = new StringBuilder(); sb.AppendLine("Date,Category,Level,User,Message"); foreach (var l in _allLogs) sb.AppendLine($"\"{l.CreatedAt}\",\"{l.Category}\",\"{l.LogLevel}\",\"{l.Operator}\",\"{l.Message.Replace("\"", "'").Replace("\n", " ")}\""); File.WriteAllText(sfd.FileName, sb.ToString()); ToastNotification.ShowSuccess("Logs exported successfully."); } } }
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) { if (_grid.Rows[e.RowIndex].DataBoundItem is LogEntryDTO log) { if (log.LogLevel == "CRITICAL" || log.Category == "Error") e.CellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.Error; else if (log.Category == "Security") e.CellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.Warning; else if (log.Category == "Network") e.CellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.Primary; } }
        private void ApplyTheme() { this.BackColor = ThemeManager.CurrentTheme.Palette.Background; _toolbar.BackColor = ThemeManager.CurrentTheme.Palette.Background; _lblCount.ForeColor = ThemeManager.CurrentTheme.Palette.TextSecondary; _txtSearch.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _txtSearch.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _cboCategory.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _cboCategory.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _grid.BackgroundColor = ThemeManager.CurrentTheme.Palette.Surface; _grid.GridColor = ThemeManager.CurrentTheme.Palette.Border; _grid.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _grid.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _grid.DefaultCellStyle.SelectionBackColor = ThemeManager.CurrentTheme.Palette.Primary; _grid.DefaultCellStyle.SelectionForeColor = Color.White; _grid.DefaultCellStyle.Font = new Font("Segoe UI", 12F); _grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Primary; _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 13F, FontStyle.Bold); foreach (Control c in _toolbar.Controls) if (c is IconButton btn) { btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface; btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary; } }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}