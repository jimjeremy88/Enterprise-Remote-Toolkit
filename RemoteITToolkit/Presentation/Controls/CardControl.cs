using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Controls
{
    public class CardControl : UserControl
    {
        private Label _lblValue;
        private IconPictureBox _iconBox;
        private Panel _progressTrack;
        private Panel _progressFill;

        public CardControl(string title, string value, IconChar icon, bool showProgress = false)
        {
            this.Margin = new Padding(15);
            this.Padding = new Padding(5, 0, 0, 0); // Left accent bar
            this.AutoSize = true;
            this.MinimumSize = new Size(350, 160);

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 2,
                RowCount = showProgress ? 3 : 2,
                AutoSize = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblTitle = new Label { Text = title.ToUpper(), Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), AutoSize = true };
            _iconBox = new IconPictureBox { IconChar = icon, IconSize = 36, Size = new Size(36, 36), BackColor = Color.Transparent };
            _lblValue = new Label { Text = value, Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 15, 0, 0) };

            tlp.Controls.Add(lblTitle, 0, 0);
            tlp.Controls.Add(_iconBox, 1, 0);
            tlp.Controls.Add(_lblValue, 0, 1);
            tlp.SetColumnSpan(_lblValue, 2);

            if (showProgress)
            {
                tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _progressTrack = new Panel { Dock = DockStyle.Fill, Height = 8, Margin = new Padding(0, 15, 0, 0) };
                _progressFill = new Panel { Dock = DockStyle.Left, Width = 0 };
                _progressTrack.Controls.Add(_progressFill);
                tlp.Controls.Add(_progressTrack, 0, 2);
                tlp.SetColumnSpan(_progressTrack, 2);
            }

            this.Controls.Add(tlp);
            ApplyTheme();
            ThemeManager.ThemeChanged += ApplyTheme;
        }

        public void UpdateValue(string newValue, int progressPercentage = -1)
        {
            if (this.IsHandleCreated)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    _lblValue.Text = newValue;
                    if (_progressFill != null && progressPercentage >= 0)
                    {
                        _progressFill.Width = (int)((progressPercentage / 100.0) * _progressTrack.Width);
                        _progressFill.BackColor = progressPercentage > 85 ? ThemeManager.CurrentTheme.Palette.Error :
                                                  progressPercentage > 70 ? ThemeManager.CurrentTheme.Palette.Warning :
                                                  ThemeManager.CurrentTheme.Palette.Primary;
                    }
                }));
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.Palette.Primary; // Accent bar
            this.Controls[0].BackColor = ThemeManager.CurrentTheme.Palette.Surface; // Inner bg
            _iconBox.IconColor = ThemeManager.CurrentTheme.Palette.Primary;
            ((TableLayoutPanel)this.Controls[0]).Controls[0].ForeColor = ThemeManager.CurrentTheme.Palette.TextSecondary; // Title
            _lblValue.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;
            if (_progressTrack != null) _progressTrack.BackColor = ThemeManager.CurrentTheme.Palette.Background;
        }

        protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= ApplyTheme; base.Dispose(disposing); }
    }
}