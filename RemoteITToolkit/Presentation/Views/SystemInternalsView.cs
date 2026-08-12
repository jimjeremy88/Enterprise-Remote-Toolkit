using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Views
{
    public class SystemInternalsView : UserControl
    {
        private readonly ISystemQueryService _queryService;
        private DataGridView _grid;
        private ComboBox _cboView;
        private Label _lblUpdateStatus;
        private FlowLayoutPanel _toolbar;
        private IconButton _btnRefresh;

        public SystemInternalsView(ISystemQueryService queryService)
        {
            _queryService = queryService;
            InitializeComponent();
            ThemeManager.ThemeChanged += ApplyTheme;
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(30);

            // FIXED: Upgraded toolbar to FlowLayoutPanel so elements automatically space themselves
            _toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };

            _cboView = new ComboBox { Width = 350, Font = new Font("Segoe UI", 14F), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 20, 0) };
            _cboView.Items.AddRange(new[] { "Startup Programs", "Scheduled Tasks" });
            _cboView.SelectedIndex = 0;
            _cboView.SelectedIndexChanged += async (s, e) => await LoadDataAsync();

            _btnRefresh = CreateToolbarButton("Refresh", IconChar.Sync);
            _btnRefresh.Click += async (s, e) => await LoadDataAsync();

            _lblUpdateStatus = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 12F), Margin = new Padding(20, 12, 0, 0) };

            _toolbar.Controls.AddRange(new Control[] { _cboView, _btnRefresh, _lblUpdateStatus });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 50 },
                ColumnHeadersHeight = 50,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _grid.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            this.Controls.Add(_grid);
            this.Controls.Add(_toolbar);
            ApplyTheme();
        }

        // FIXED: Added TextImageRelation & auto-spacing helper
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
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private async Task LoadDataAsync()
        {
            _lblUpdateStatus.Text = "Loading Windows Update Status...";
            _lblUpdateStatus.Text = await _queryService.GetWindowsUpdateStatusAsync();

            _grid.DataSource = null;
            if (_cboView.SelectedIndex == 0) _grid.DataSource = (await _queryService.GetStartupProgramsAsync()).ToList();
            else _grid.DataSource = (await _queryService.GetScheduledTasksAsync()).ToList();
        }

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.Palette.Background;
            _toolbar.BackColor = ThemeManager.CurrentTheme.Palette.Background;
            _lblUpdateStatus.ForeColor = ThemeManager.CurrentTheme.Palette.Primary;

            _cboView.BackColor = ThemeManager.CurrentTheme.Palette.Surface;
            _cboView.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;

            _grid.BackgroundColor = ThemeManager.CurrentTheme.Palette.Surface;
            _grid.GridColor = ThemeManager.CurrentTheme.Palette.Border;
            _grid.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Surface;
            _grid.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            _grid.DefaultCellStyle.SelectionBackColor = ThemeManager.CurrentTheme.Palette.Primary;
            _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 12F);

            _grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.Palette.Primary;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);

            foreach (Control c in _toolbar.Controls)
            {
                if (c is IconButton btn)
                {
                    btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface;
                    btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
                    btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary;
                }
            }
        }

        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}