using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.DTOs;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Forms;
using RemoteITToolkit.Presentation.Helpers;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Views
{
    public class EventLogView : UserControl
    {
        private readonly ISystemQueryService _queryService;
        private List<SystemEventLogDTO> _allLogs;
        private DataGridView _grid;
        private TextBox _txtSearch;
        private ComboBox _cboLogType, _cboLevel;
        private FlowLayoutPanel _toolbar;
        private Label _lblCount;

        public EventLogView(ISystemQueryService queryService)
        {
            _queryService = queryService; _allLogs = new List<SystemEventLogDTO>();
            InitializeComponent(); ApplyTheme(); ThemeManager.ThemeChanged += ApplyTheme; _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill; this.Padding = new Padding(30);

            // FIXED: Using FlowLayoutPanel to dynamically space the dropdowns and buttons
            _toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };

            _cboLogType = new ComboBox { Width = 220, Font = new Font("Segoe UI", 14F), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 20, 0) };
            _cboLogType.Items.AddRange(new object[] { "System", "Application", "Security" });
            _cboLogType.SelectedIndex = 0;
            _cboLogType.SelectedIndexChanged += async (s, e) => await LoadDataAsync();

            _cboLevel = new ComboBox { Width = 220, Font = new Font("Segoe UI", 14F), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 20, 0) };
            _cboLevel.Items.AddRange(new object[] { "All Levels", "Error & Critical", "Warning", "Information" });
            _cboLevel.SelectedIndex = 0;
            _cboLevel.SelectedIndexChanged += (s, e) => FilterData();

            _txtSearch = new TextBox { Width = 300, Font = new Font("Segoe UI", 14F), Text = "Search message...", Margin = new Padding(0, 5, 20, 0) };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Search message...") _txtSearch.Text = ""; };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) _txtSearch.Text = "Search message..."; };
            _txtSearch.TextChanged += (s, e) => { if (_txtSearch.Text != "Search message...") FilterData(); };

            var btnRefresh = CreateToolbarButton("Refresh", IconChar.Sync); btnRefresh.Click += async (s, e) => await LoadDataAsync();
            var btnCsv = CreateToolbarButton("CSV", IconChar.FileCsv); btnCsv.Click += (s, e) => ExportData("csv");
            var btnPdf = CreateToolbarButton("PDF", IconChar.FilePdf); btnPdf.Click += (s, e) => ExportData("pdf");

            _lblCount = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), Margin = new Padding(20, 8, 0, 0) };

            _toolbar.Controls.AddRange(new Control[] { _cboLogType, _cboLevel, _txtSearch, btnRefresh, btnCsv, btnPdf, _lblCount });

            _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = true, RowHeadersVisible = false, EnableHeadersVisualStyles = false, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, RowTemplate = { Height = 50 }, ColumnHeadersHeight = 50 };
            _grid.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EntryType", HeaderText = "Level", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TimeGenerated", HeaderText = "Date and Time", Width = 200 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Source", HeaderText = "Source", Width = 220 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EventId", HeaderText = "Event ID", Width = 100 });
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

        private async Task LoadDataAsync() { string logName = ""; this.SafeInvoke(() => { logName = _cboLogType.SelectedItem.ToString(); _grid.DataSource = null; _lblCount.Text = "Loading..."; _txtSearch.Enabled = false; }); try { var data = await _queryService.GetRecentEventLogsAsync(logName, 500); _allLogs = data.ToList(); FilterData(); } catch (Exception ex) { this.SafeInvoke(() => ToastNotification.Show(ex.Message, IconChar.ExclamationTriangle, ThemeManager.CurrentTheme.Palette.Error)); } finally { this.SafeInvoke(() => _txtSearch.Enabled = true); } }
        private void FilterData() { string term = _txtSearch.Text.Trim(); string levelFilter = _cboLevel.SelectedItem.ToString(); var filtered = _allLogs.AsEnumerable(); if (levelFilter == "Error & Critical") filtered = filtered.Where(l => l.EntryType.Contains("Error") || l.EntryType.Contains("Critical")); else if (levelFilter == "Warning") filtered = filtered.Where(l => l.EntryType.Contains("Warning")); else if (levelFilter == "Information") filtered = filtered.Where(l => l.EntryType.Contains("Information")); if (!string.IsNullOrEmpty(term) && term != "Search message...") { filtered = filtered.Where(l => l.Message.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 || l.Source.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0); } var finalResult = filtered.ToList(); _grid.DataSource = new SortableBindingList<SystemEventLogDTO>(finalResult); _lblCount.Text = $"{finalResult.Count} Events"; }
        private void ExportData(string type) { using (var sfd = new SaveFileDialog { Filter = type == "csv" ? "CSV Files|*.csv" : "PDF Files|*.pdf", FileName = $"EventLog_{DateTime.Now:yyyyMMdd}.{type}" }) { if (sfd.ShowDialog() == DialogResult.OK) { var sb = new StringBuilder(); if (type == "csv") { sb.AppendLine("Level,Date,Source,EventID,Message"); foreach (var l in _allLogs) sb.AppendLine($"\"{l.EntryType}\",\"{l.TimeGenerated}\",\"{l.Source}\",\"{l.EventId}\",\"{l.Message.Replace("\"", "'").Replace("\n", " ")}\""); } else { sb.AppendLine("%PDF-1.4\n1 0 obj <</Type /Catalog /Pages 2 0 R>> endobj\n2 0 obj <</Type /Pages /Kids [3 0 R] /Count 1>> endobj\n3 0 obj <</Type /Page /Parent 2 0 R /Resources <</Font <</F1 4 0 R>>>> /MediaBox [0 0 612 792] /Contents 5 0 R>> endobj\n4 0 obj <</Type /Font /Subtype /Type1 /BaseFont /Helvetica>> endobj\n5 0 obj <</Length 200>> stream\nBT /F1 10 Tf 50 750 Td\n"); sb.AppendLine($"(Event Log Report - {DateTime.Now}) Tj T* T* "); int count = 0; foreach (var l in _allLogs.Where(x => x.EntryType == "Error" || x.EntryType == "Critical").Take(30)) { string safeMsg = l.Message.Replace("(", "[").Replace(")", "]").Replace("\n", " ").Replace("\r", " "); safeMsg = safeMsg.Length > 80 ? safeMsg.Substring(0, 77) + "..." : safeMsg; sb.AppendLine($"({l.TimeGenerated} - {l.Source}: {safeMsg}) Tj T* "); if (++count > 25) break; } sb.AppendLine("ET\nendstream endobj\nxref\n0 6\n0000000000 65535 f \n0000000009 00000 n \n0000000056 00000 n \n0000000111 00000 n \n0000000212 00000 n \n0000000279 00000 n \ntrailer <</Size 6 /Root 1 0 R>>\nstartxref\n380\n%%EOF"); } File.WriteAllText(sfd.FileName, sb.ToString()); ToastNotification.ShowSuccess($"Successfully exported to {type.ToUpper()}"); } } }
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) { if (_grid.Rows[e.RowIndex].DataBoundItem is SystemEventLogDTO log) { if (log.EntryType == "Error" || log.EntryType == "Critical") { e.CellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.Error; e.CellStyle.Font = new Font(_grid.DefaultCellStyle.Font, FontStyle.Bold); } else if (log.EntryType == "Warning") { e.CellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.Warning; } } }
        private void ApplyTheme() { this.BackColor = ThemeManager.CurrentTheme.Palette.Background; _toolbar.BackColor = ThemeManager.CurrentTheme.Palette.Background; _lblCount.ForeColor = ThemeManager.CurrentTheme.Palette.TextSecondary; _txtSearch.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _txtSearch.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _cboLogType.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _cboLogType.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _cboLevel.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _cboLevel.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _grid.BackgroundColor = ThemeManager.CurrentTheme.Palette.Surface; _grid.GridColor = ThemeManager.CurrentTheme.Palette.Border; _grid.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _grid.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _grid.DefaultCellStyle.SelectionBackColor = ThemeManager.CurrentTheme.Palette.Primary; _grid.DefaultCellStyle.SelectionForeColor = Color.White; _grid.DefaultCellStyle.Font = new Font("Segoe UI", 12F); _grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Primary; _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 13F, FontStyle.Bold); foreach (Control c in _toolbar.Controls) if (c is IconButton btn) { btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface; btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary; } }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}