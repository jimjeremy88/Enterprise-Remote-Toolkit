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
    public class WindowsServicesView : UserControl
    {
        private readonly ISystemQueryService _queryService;
        private readonly IWindowsToolsService _toolsService;
        private List<WindowsServiceDTO> _allServices;

        private DataGridView _grid;
        private TextBox _txtSearch;
        private FlowLayoutPanel _toolbar;
        private Label _lblCount;
        private FlowLayoutPanel _adminWarningBanner;

        public WindowsServicesView(ISystemQueryService queryService, IWindowsToolsService toolsService)
        {
            _queryService = queryService; _toolsService = toolsService; _allServices = new List<WindowsServiceDTO>();
            InitializeComponent(); ApplyTheme(); ThemeManager.ThemeChanged += ApplyTheme; _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill; this.Padding = new Padding(30);

            _adminWarningBanner = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(15), Visible = !_toolsService.IsAdministrator(), WrapContents = false };
            var lblWarning = new Label { Text = "⚠ Standard User: You may view services, but Start/Stop actions require Application Restart as Administrator.", Font = new Font("Segoe UI Semibold", 12F), AutoSize = true };
            _adminWarningBanner.Controls.Add(lblWarning);

            var spacer = new Panel { Dock = DockStyle.Top, Height = 20 };

            _toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };

            _txtSearch = new TextBox { Width = 350, Font = new Font("Segoe UI", 14F), Text = "Search services...", Margin = new Padding(0, 5, 20, 0) };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Search services...") _txtSearch.Text = ""; };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) _txtSearch.Text = "Search services..."; };
            _txtSearch.TextChanged += (s, e) => { if (_txtSearch.Text != "Search services...") FilterData(); };

            var btnStart = CreateToolbarButton("Start", IconChar.Play); btnStart.Click += async (s, e) => await ExecuteServiceAction("Start");
            var btnStop = CreateToolbarButton("Stop", IconChar.Stop); btnStop.Click += async (s, e) => await ExecuteServiceAction("Stop");
            var btnRestart = CreateToolbarButton("Restart", IconChar.Redo); btnRestart.Click += async (s, e) => await ExecuteServiceAction("Restart");
            var btnRefresh = CreateToolbarButton("Refresh", IconChar.Sync); btnRefresh.Click += async (s, e) => await LoadDataAsync();
            var btnCsv = CreateToolbarButton("CSV", IconChar.FileCsv); btnCsv.Click += (s, e) => ExportData();

            _lblCount = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), Margin = new Padding(20, 8, 0, 0) };

            _toolbar.Controls.AddRange(new Control[] { _txtSearch, btnStart, btnStop, btnRestart, btnRefresh, btnCsv, _lblCount });

            _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false, EnableHeadersVisualStyles = false, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, RowTemplate = { Height = 50 }, ColumnHeadersHeight = 50 };
            _grid.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DisplayName", HeaderText = "Display Name", Width = 400 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Service Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StartMode", HeaderText = "Startup Type", Width = 200 });

            _grid.CellFormatting += Grid_CellFormatting;

            this.Controls.Add(_grid);
            this.Controls.Add(_toolbar);
            this.Controls.Add(spacer);
            this.Controls.Add(_adminWarningBanner);
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

        private async Task LoadDataAsync() { this.SafeInvoke(() => { _grid.DataSource = null; _lblCount.Text = "Loading..."; _txtSearch.Enabled = false; }); try { var data = await _queryService.GetWindowsServicesAsync(); _allServices = data.ToList(); FilterData(); } catch (Exception ex) { this.SafeInvoke(() => ToastNotification.Show(ex.Message, IconChar.ExclamationTriangle, ThemeManager.CurrentTheme.Palette.Error)); } finally { this.SafeInvoke(() => _txtSearch.Enabled = true); } }
        private void FilterData() { string term = _txtSearch.Text.Trim(); var filtered = string.IsNullOrEmpty(term) || term == "Search services..." ? _allServices : _allServices.Where(s => s.DisplayName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 || s.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0).ToList(); _grid.DataSource = new SortableBindingList<WindowsServiceDTO>(filtered); _lblCount.Text = $"{filtered.Count} Services"; }
        private async Task ExecuteServiceAction(string action) { if (!_toolsService.IsAdministrator()) { ToastNotification.Show("Access Denied. Run as Admin.", IconChar.Lock, ThemeManager.CurrentTheme.Palette.Warning); return; } if (_grid.SelectedRows.Count == 0) return; var service = _grid.SelectedRows[0].DataBoundItem as WindowsServiceDTO; if (service == null) return; ToastNotification.ShowInfo($"{action}ing {service.Name}..."); bool success = false; try { switch (action) { case "Start": success = await _queryService.StartServiceAsync(service.Name); break; case "Stop": success = await _queryService.StopServiceAsync(service.Name); break; case "Restart": success = await _queryService.RestartServiceAsync(service.Name); break; } if (success) { ToastNotification.ShowSuccess($"Service {action.ToLower()}ed."); await LoadDataAsync(); } else { ToastNotification.Show("Action failed.", IconChar.TimesCircle, ThemeManager.CurrentTheme.Palette.Error); } } catch (Exception ex) { ToastNotification.Show($"Error: {ex.Message}", IconChar.TimesCircle, ThemeManager.CurrentTheme.Palette.Error); } }
        private void ExportData() { using (var sfd = new SaveFileDialog { Filter = "CSV Files|*.csv", FileName = $"Services_{DateTime.Now:yyyyMMdd}.csv" }) { if (sfd.ShowDialog() == DialogResult.OK) { var sb = new StringBuilder(); sb.AppendLine("DisplayName,ServiceName,Status,StartupType"); foreach (var s in _allServices) sb.AppendLine($"\"{s.DisplayName}\",\"{s.Name}\",\"{s.Status}\",\"{s.StartMode}\""); File.WriteAllText(sfd.FileName, sb.ToString()); ToastNotification.ShowSuccess("Exported!"); } } }
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) { if (e.ColumnIndex == 2 && e.Value != null) { string status = e.Value.ToString(); if (status == "Running") e.CellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.Success; else if (status == "Stopped") e.CellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.Error; else e.CellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.Warning; } }

        private void ApplyTheme() { this.BackColor = ThemeManager.CurrentTheme.Palette.Background; _toolbar.BackColor = ThemeManager.CurrentTheme.Palette.Background; _lblCount.ForeColor = ThemeManager.CurrentTheme.Palette.TextSecondary; _txtSearch.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _txtSearch.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _adminWarningBanner.BackColor = ThemeManager.CurrentTheme.Palette.Warning; _adminWarningBanner.Controls[0].ForeColor = Color.White; _grid.BackgroundColor = ThemeManager.CurrentTheme.Palette.Surface; _grid.GridColor = ThemeManager.CurrentTheme.Palette.Border; _grid.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _grid.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _grid.DefaultCellStyle.SelectionBackColor = ThemeManager.CurrentTheme.Palette.Primary; _grid.DefaultCellStyle.SelectionForeColor = Color.White; _grid.DefaultCellStyle.Font = new Font("Segoe UI", 12F); _grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Primary; _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 13F, FontStyle.Bold); foreach (Control c in _toolbar.Controls) if (c is IconButton btn) { btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface; btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary; } }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}