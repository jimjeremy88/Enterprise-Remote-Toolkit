using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Forms;
using RemoteITToolkit.Presentation.Helpers;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Views
{
    public class ReportGeneratorView : UserControl
    {
        private readonly IReportGeneratorService _reportService;
        private TextBox _txtTechName, _txtCompany;
        private CheckBox _chkApps, _chkServices, _chkEvents;
        private IconButton _btnGenerate;
        private Label _lblStatus;
        private ProgressBar _progressBar;

        public ReportGeneratorView(IReportGeneratorService reportService)
        {
            _reportService = reportService; InitializeComponent(); ApplyTheme(); ThemeManager.ThemeChanged += ApplyTheme;
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill; this.Padding = new Padding(40);

            var pnlForm = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };

            var lblDesc = new Label { Text = "Generate a comprehensive PDF audit containing full system telemetry, hardware diagnostics, and security logs.", Font = new Font("Segoe UI", 12F), AutoSize = true, Margin = new Padding(0, 0, 0, 30) };
            pnlForm.Controls.Add(lblDesc);

            pnlForm.Controls.Add(CreateLabel("Technician Name:", new Padding(0, 10, 0, 5)));
            _txtTechName = new TextBox { Width = 400, Font = new Font("Segoe UI", 14F), Margin = new Padding(0, 0, 0, 20) };
            pnlForm.Controls.Add(_txtTechName);

            pnlForm.Controls.Add(CreateLabel("Client / Company Name:", new Padding(0, 10, 0, 5)));
            _txtCompany = new TextBox { Width = 400, Font = new Font("Segoe UI", 14F), Text = "Enterprise IT Solutions", Margin = new Padding(0, 0, 0, 30) };
            pnlForm.Controls.Add(_txtCompany);

            pnlForm.Controls.Add(CreateLabel("Report Inclusions:", new Padding(0, 10, 0, 15)));

            // FIXED: CHECKBOXES NOW USE AUTOSIZE SO THEY DON'T DISAPPEAR!
            _chkApps = new CheckBox { Text = " Include Installed Software", AutoSize = true, Font = new Font("Segoe UI", 12F), Checked = true, Margin = new Padding(0, 0, 0, 10) };
            _chkServices = new CheckBox { Text = " Include Running Services", AutoSize = true, Font = new Font("Segoe UI", 12F), Checked = true, Margin = new Padding(0, 0, 0, 10) };
            _chkEvents = new CheckBox { Text = " Include Critical Event Logs", AutoSize = true, Font = new Font("Segoe UI", 12F), Checked = true, Margin = new Padding(0, 0, 0, 30) };
            pnlForm.Controls.AddRange(new Control[] { _chkApps, _chkServices, _chkEvents });

            _btnGenerate = new IconButton { Text = "  Generate PDF Report", IconChar = IconChar.FilePdf, IconSize = 24, Width = 300, Height = 60, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 12F), TextImageRelation = TextImageRelation.ImageBeforeText, Margin = new Padding(0, 0, 0, 20) };
            _btnGenerate.FlatAppearance.BorderSize = 0; _btnGenerate.Click += BtnGenerate_Click;
            pnlForm.Controls.Add(_btnGenerate);

            _progressBar = new ProgressBar { Width = 400, Height = 10, Style = ProgressBarStyle.Marquee, Visible = false, Margin = new Padding(0, 0, 0, 10) };
            _lblStatus = new Label { AutoSize = true, Font = new Font("Segoe UI", 11F), ForeColor = ThemeManager.CurrentTheme.Palette.TextSecondary };

            pnlForm.Controls.Add(_progressBar);
            pnlForm.Controls.Add(_lblStatus);

            this.Controls.Add(pnlForm);
        }

        private Label CreateLabel(string text, Padding margin) => new Label { Text = text, Margin = margin, Font = new Font("Segoe UI Semibold", 12F), AutoSize = true };

        private async void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtTechName.Text)) { ToastNotification.Show("Please enter a Technician Name.", IconChar.ExclamationCircle, ThemeManager.CurrentTheme.Palette.Warning); return; }
            this.SafeInvoke(() => { _btnGenerate.Enabled = false; _progressBar.Visible = true; _lblStatus.Text = "Querying system telemetry and generating PDF..."; });
            try
            {
                string path = await _reportService.GenerateEnterpriseReportAsync(_txtTechName.Text.Trim(), _txtCompany.Text.Trim(), _chkApps.Checked, _chkServices.Checked, _chkEvents.Checked);
                ToastNotification.ShowSuccess("Report generated successfully!"); _lblStatus.Text = "Report saved to: " + path;
                if (MessageBox.Show("Report generated successfully. Would you like to open it now?", "Report Complete", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes) Process.Start(path);
            }
            catch (Exception ex) { ToastNotification.Show(ex.Message, IconChar.TimesCircle, ThemeManager.CurrentTheme.Palette.Error); _lblStatus.Text = "Generation failed."; }
            finally { this.SafeInvoke(() => { _btnGenerate.Enabled = true; _progressBar.Visible = false; }); }
        }

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.Palette.Background;
            _txtTechName.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _txtTechName.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            _txtCompany.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _txtCompany.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            _chkApps.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _chkServices.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; _chkEvents.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            _btnGenerate.BackColor = ThemeManager.CurrentTheme.Palette.Primary; _btnGenerate.ForeColor = Color.White; _btnGenerate.IconColor = Color.White;

            foreach (Control c in this.Controls[0].Controls)
            {
                if (c is Label lbl && lbl != _lblStatus) lbl.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}