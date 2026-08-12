using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Core.Interfaces;
using RemoteITToolkit.Presentation.Helpers;
using RemoteITToolkit.Presentation.Theme;
using RemoteITToolkit.Presentation.Forms;

namespace RemoteITToolkit.Presentation.Views
{
    public class NetworkDiagnosticView : UserControl
    {
        private readonly INetworkToolsService _netTools;
        private FlowLayoutPanel _topBar;
        private TextBox _txtTarget;
        private ComboBox _cboTool;
        private IconButton _btnExecute;
        private RichTextBox _rtbConsole;
        private ProgressBar _progressBar;
        private FlowLayoutPanel _actionPanel;

        public NetworkDiagnosticView(INetworkToolsService netTools)
        {
            _netTools = netTools;
            InitializeComponent();
            ThemeManager.ThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(30);

            _topBar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20), WrapContents = false };

            var lblTarget = new Label { Text = "Target IP / Domain:", Font = new Font("Segoe UI Semibold", 14F), AutoSize = true, Margin = new Padding(0, 8, 10, 0) };
            _txtTarget = new TextBox { Width = 300, Font = new Font("Segoe UI", 14F), Text = "google.com", Margin = new Padding(0, 5, 20, 0) };

            _cboTool = new ComboBox { Width = 220, Font = new Font("Segoe UI", 14F), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 20, 0) };
            _cboTool.Items.AddRange(new object[] { "Ping", "Traceroute", "DNS Lookup", "Whois Lookup", "Port Scan", "Latency Test" });
            _cboTool.SelectedIndex = 0;

            // FIXED: Added TextImageRelation to the Execute button
            _btnExecute = new IconButton { Text = "  Execute", IconChar = IconChar.Play, IconSize = 24, AutoSize = true, MinimumSize = new Size(150, 45), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Margin = new Padding(0, 3, 0, 0), TextImageRelation = TextImageRelation.ImageBeforeText, ImageAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0) };
            _btnExecute.FlatAppearance.BorderSize = 0;
            _btnExecute.Click += BtnExecute_Click;

            _topBar.Controls.AddRange(new Control[] { lblTarget, _txtTarget, _cboTool, _btnExecute });

            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(0, 20, 0, 0) };
            _progressBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 5, Style = ProgressBarStyle.Marquee, Visible = false };

            _actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            _actionPanel.Controls.Add(CreateActionButton("IP Config", IconChar.NetworkWired, () => RunShellCommand("/all")));
            _actionPanel.Controls.Add(CreateActionButton("Flush DNS", IconChar.Broom, () => RunShellCommand("/flushdns")));
            _actionPanel.Controls.Add(CreateActionButton("Release IP", IconChar.Eject, () => RunShellCommand("/release")));
            _actionPanel.Controls.Add(CreateActionButton("Renew IP", IconChar.SyncAlt, () => RunShellCommand("/renew")));
            _actionPanel.Controls.Add(CreateActionButton("Speed Test", IconChar.TachometerAlt, RunSpeedTest));
            _actionPanel.Controls.Add(CreateActionButton("Copy Results", IconChar.Copy, CopyToClipboard));

            bottomBar.Controls.Add(_actionPanel);
            bottomBar.Controls.Add(_progressBar);

            var consoleContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 20, 0, 20) };
            _rtbConsole = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 12F), ReadOnly = true, BorderStyle = BorderStyle.None, Padding = new Padding(15) };
            consoleContainer.Controls.Add(_rtbConsole);

            this.Controls.Add(consoleContainer);
            this.Controls.Add(bottomBar);
            this.Controls.Add(_topBar);
        }

        // FIXED: Added TextImageRelation to force the Icon to the left of the Text!
        private IconButton CreateActionButton(string text, IconChar icon, Action action)
        {
            var btn = new IconButton
            {
                Text = $"  {text}",
                IconChar = icon,
                IconSize = 24,
                AutoSize = true,
                MinimumSize = new Size(160, 50),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 12F),
                Margin = new Padding(0, 0, 15, 0),
                Padding = new Padding(10, 5, 10, 5),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += async (s, e) => { try { action(); } catch { } };
            return btn;
        }

        private async void BtnExecute_Click(object sender, EventArgs e)
        {
            string target = _txtTarget.Text.Trim();
            string tool = _cboTool.SelectedItem.ToString();
            if (string.IsNullOrEmpty(target)) { ToastNotification.ShowInfo("Please enter a valid target."); return; }

            await RunNetworkTaskAsync($"Running {tool} against {target}...", async () =>
            {
                switch (tool)
                {
                    case "Ping": var p = await _netTools.PingHostAsync(target); AppendConsole($"[{p.Status}] {p.Address} - Time: {p.RoundtripTime}ms\n", p.Status == "Success" ? ThemeManager.CurrentTheme.Palette.Success : ThemeManager.CurrentTheme.Palette.Error); break;
                    case "Traceroute": var hops = await _netTools.TracerouteAsync(target); foreach (var h in hops) AppendConsole($"Hop {h.Hop,-3} | {h.Time,-4}ms | {h.IpAddress}\n", ThemeManager.CurrentTheme.Palette.TextPrimary); break;
                    case "DNS Lookup": string dns = await _netTools.ResolveDnsAsync(target); AppendConsole(dns + "\n", ThemeManager.CurrentTheme.Palette.Primary); break;
                    case "Whois Lookup": string whois = await _netTools.RunWhoisAsync(target); AppendConsole(whois + "\n", ThemeManager.CurrentTheme.Palette.TextPrimary); break;
                    case "Port Scan": string ports = await _netTools.ScanCommonPortsAsync(target); AppendConsole(ports + "\n", ThemeManager.CurrentTheme.Palette.Warning); break;
                    case "Latency Test": string latency = await _netTools.TestLatencyAsync(target); AppendConsole(latency + "\n", ThemeManager.CurrentTheme.Palette.Success); break;
                }
            });
        }

        private async void RunShellCommand(string arg) => await RunNetworkTaskAsync($"Executing ipconfig {arg}...", async () => { string result = await _netTools.ExecuteIpConfigCommandAsync(arg); AppendConsole(result + "\n", ThemeManager.CurrentTheme.Palette.TextPrimary); });
        private async void RunSpeedTest() => await RunNetworkTaskAsync("Simulating Bandwidth Download...", async () => { string result = await _netTools.SimulateBandwidthAsync(); AppendConsole(result + "\n", ThemeManager.CurrentTheme.Palette.Primary); });
        private void CopyToClipboard() { if (!string.IsNullOrWhiteSpace(_rtbConsole.Text)) { Clipboard.SetText(_rtbConsole.Text); ToastNotification.ShowSuccess("Copied to clipboard!"); } }

        private async Task RunNetworkTaskAsync(string startMessage, Func<Task> task)
        {
            this.SafeInvoke(() => { _btnExecute.Enabled = false; _progressBar.Visible = true; AppendConsole($"\n{startMessage}\n", ThemeManager.CurrentTheme.Palette.TextSecondary); });
            try { await task(); }
            catch (Exception ex) { AppendConsole($"Error: {ex.Message}\n", ThemeManager.CurrentTheme.Palette.Error); }
            finally { this.SafeInvoke(() => { _btnExecute.Enabled = true; _progressBar.Visible = false; }); }
        }

        private void AppendConsole(string text, Color color) { this.SafeInvoke(() => { _rtbConsole.SelectionStart = _rtbConsole.TextLength; _rtbConsole.SelectionLength = 0; _rtbConsole.SelectionColor = color; _rtbConsole.AppendText(text); _rtbConsole.ScrollToCaret(); }); }

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.Palette.Background; _topBar.BackColor = ThemeManager.CurrentTheme.Palette.Background;
            _btnExecute.BackColor = ThemeManager.CurrentTheme.Palette.Primary; _btnExecute.ForeColor = Color.White; _btnExecute.IconColor = Color.White;
            _txtTarget.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _txtTarget.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            _cboTool.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _cboTool.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            _rtbConsole.BackColor = ThemeManager.CurrentTheme.Palette.Surface; _rtbConsole.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            foreach (Control c in _topBar.Controls) if (c is Label lbl) lbl.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            foreach (Control c in _actionPanel.Controls) if (c is IconButton btn) { btn.BackColor = ThemeManager.CurrentTheme.Palette.Surface; btn.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary; btn.IconColor = ThemeManager.CurrentTheme.Palette.Primary; btn.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.Palette.Border; }
        }
        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}