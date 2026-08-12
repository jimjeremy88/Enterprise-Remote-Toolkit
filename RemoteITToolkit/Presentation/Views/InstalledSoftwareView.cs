using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class InstalledSoftwareView : UserControl
    {
        private readonly ISystemQueryService _queryService;
        private List<InstalledProgramDTO> _allSoftware;
        private DataGridView _grid;
        private TextBox _txtSearch;
        private FlowLayoutPanel _toolbar;
        private Label _lblCount;

        public InstalledSoftwareView(ISystemQueryService queryService)
        {
            _queryService = queryService; _allSoftware = new List<InstalledProgramDTO>();
            InitializeComponent(); ApplyTheme(); ThemeManager.ThemeChanged += ApplyTheme; _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill; this.Padding = new Padding(30);

            _toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };

            _txtSearch = new TextBox { Width = 400, Font = new Font("Segoe UI", 14F), Text = "Search software...", Margin = new Padding(0, 5, 20, 0) };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Search software...") _txtSearch.Text = ""; };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) _txtSearch.Text = "Search software..."; };
            _txtSearch.TextChanged += (s, e) => { if (_txtSearch.Text != "Search software...") FilterData(); };

            var btnRefresh = CreateToolbarButton("Refresh", IconChar.Sync); btnRefresh.Click += async (s, e) => await LoadDataAsync();
            var btnCopy = CreateToolbarButton("Copy", IconChar.Copy); btnCopy.Click += BtnCopy_Click;
            var btnCsv = CreateToolbarButton("CSV", IconChar.FileCsv); btnCsv.Click += (s, e) => ExportData("csv");
            var btnPdf = CreateToolbarButton("PDF", IconChar.FilePdf); btnPdf.Click += (s, e) => ExportData("pdf");

            _lblCount = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), Margin = new Padding(20, 8, 0, 0) };

            _toolbar.Controls.AddRange(new Control[] { _txtSearch, btnRefresh, btnCopy, btnCsv, btnPdf, _lblCount });

            _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = true, RowHeadersVisible = false, EnableHeadersVisualStyles = false, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, RowTemplate = { Height = 50 }, ColumnHeadersHeight = 50 };
            _grid.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Application Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Publisher", HeaderText = "Publisher", Width = 300 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Version", HeaderText = "Version", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InstallDate", HeaderText = "Install Date", Width = 150 });

            this.Controls.Add(_grid);
            this.Controls.Add(_toolbar);
        }

        // FIXED: Added TextImageRelation and Alignments so Icon never overlaps Text!
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

        private async Task LoadDataAsync() { this.SafeInvoke(() => { _grid.DataSource = null; _lblCount.Text = "Loading..."; _txtSearch.Enabled = false; }); try { var data = await _queryService.GetInstalledProgramsAsync(); _allSoftware = data.ToList(); FilterData(); } catch (Exception ex) { this.SafeInvoke(() => ToastNotification.Show(ex.Message, IconChar.ExclamationTriangle, ThemeManager.CurrentTheme.Palette.Error)); } finally { this.SafeInvoke(() => _txtSearch.Enabled = true); } }
        private void FilterData() { string term = _txtSearch.Text.Trim(); var filtered = string.IsNullOrEmpty(term) || term == "Search software..." ? _allSoftware : _allSoftware.Where(s => s.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 || s.Publisher.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0).ToList(); _grid.DataSource = new SortableBindingList<InstalledProgramDTO>(filtered); _lblCount.Text = $"{filtered.Count} Applications"; }
        private void BtnCopy_Click(object sender, EventArgs e) { if (_grid.GetClipboardContent() is DataObject dataObj) { Clipboard.SetDataObject(dataObj); ToastNotification.ShowSuccess("Copied!"); } }
        private void ExportData(string type) { using (var sfd = new SaveFileDialog { Filter = type == "csv" ? "CSV Files|*.csv" : "PDF Files|*.pdf", FileName = $"Software_{DateTime.Now:yyyyMMdd}.{type}" }) { if (sfd.ShowDialog() == DialogResult.OK) { var sb = new StringBuilder(); if (type == "csv") { sb.AppendLine("Name,Publisher,Version,InstallDate"); foreach (var app in _allSoftware) sb.AppendLine($"\"{app.Name}\",\"{app.Publisher}\",\"{app.Version}\",\"{app.InstallDate}\""); } else { sb.AppendLine("%PDF-1.4\n1 0 obj <</Type /Catalog /Pages 2 0 R>> endobj\n2 0 obj <</Type /Pages /Kids [3 0 R] /Count 1>> endobj\n3 0 obj <</Type /Page /Parent 2 0 R /Resources <</Font <</F1 4 0 R>>>> /MediaBox [0 0 612 792] /Contents 5 0 R>> endobj\n4 0 obj <</Type /Font /Subtype /Type1 /BaseFont /Helvetica>> endobj\n5 0 obj <</Length 200>> stream\nBT /F1 10 Tf 50 750 Td\n"); sb.AppendLine($"(Software Report) Tj T* T* "); foreach (var app in _allSoftware.Take(40)) { sb.AppendLine($"({app.Name.Replace("(", "").Replace(")", "")}) Tj T* "); } sb.AppendLine("ET\nendstream endobj\nxref\n0 6\n0000000000 65535 f \n0000000009 00000 n \n0000000056 00000 n \n0000000111 00000 n \n0000000212 00000 n \n0000000279 00000 n \ntrailer <</Size 6 /Root 1 0 R>>\nstartxref\n380\n%%EOF"); } File.WriteAllText(sfd.FileName, sb.ToString()); ToastNotification.ShowSuccess("Exported!"); } } }

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.Palette.Background; _toolbar.BackColor = ThemeManager.CurrentTheme.Palette.Background; _lblCount.ForeColor = ThemeManager.CurrentTheme.Palette.TextSecondary;
            _txtSearch.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _txtSearch.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            _grid.BackgroundColor = ThemeManager.CurrentTheme.Palette.Surface; _grid.GridColor = ThemeManager.CurrentTheme.Palette.Border; _grid.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _grid.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _grid.DefaultCellStyle.SelectionBackColor = ThemeManager.CurrentTheme.Palette.Primary; _grid.DefaultCellStyle.SelectionForeColor = Color.White; _grid.DefaultCellStyle.Font = new Font("Segoe UI", 12F); _grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Primary; _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            foreach (Control c in _toolbar.Controls) if (c is IconButton btn) { btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface; btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary; }
        }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
    public class SortableBindingList<T> : BindingList<T> { public SortableBindingList(IList<T> list) : base(list) { } protected override bool SupportsSortingCore => true; protected override bool IsSortedCore { get; } protected override ListSortDirection SortDirectionCore { get; } protected override PropertyDescriptor SortPropertyCore { get; } protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction) { var items = this.Items as List<T>; if (items == null) return; items.Sort((a, b) => { object valA = prop.GetValue(a); object valB = prop.GetValue(b); int cmp = valA == null && valB == null ? 0 : valA == null ? -1 : valB == null ? 1 : Comparer<object>.Default.Compare(valA, valB); return direction == ListSortDirection.Ascending ? cmp : -cmp; }); this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1)); } }
}