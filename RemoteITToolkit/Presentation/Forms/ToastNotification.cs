using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Forms
{
    public class ToastNotification : Form
    {
        private readonly Timer _animationTimer;
        private readonly Timer _displayTimer;
        private int _targetY;
        private int _currentY;

        public ToastNotification(string message, IconChar icon, Color iconColor)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(350, 60);
            this.BackColor = ThemeManager.CurrentTheme.Palette.Surface;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            var iconBox = new IconPictureBox { IconChar = icon, IconColor = iconColor, BackColor = Color.Transparent, Size = new Size(32, 32), Location = new Point(15, 14) };
            var lblMessage = new Label { Text = message, ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary, Font = new Font("Segoe UI", 10F), Location = new Point(60, 20), AutoSize = true };

            this.Controls.Add(iconBox);
            this.Controls.Add(lblMessage);

            var screen = Screen.PrimaryScreen.WorkingArea;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(screen.Width - this.Width - 20, screen.Height);

            _targetY = screen.Height - this.Height - 20;
            _currentY = this.Location.Y;

            _animationTimer = new Timer { Interval = 10 };
            _animationTimer.Tick += AnimationTimer_Tick;

            _displayTimer = new Timer { Interval = 3000 };
            _displayTimer.Tick += (s, e) => { _displayTimer.Stop(); _targetY = screen.Height + 10; _animationTimer.Start(); };
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_currentY > _targetY) _currentY -= 10;
            else if (_currentY < _targetY) _currentY += 10;

            this.Location = new Point(this.Location.X, _currentY);

            if (Math.Abs(_currentY - _targetY) < 10)
            {
                this.Location = new Point(this.Location.X, _targetY);
                _animationTimer.Stop();
                if (_targetY < Screen.PrimaryScreen.WorkingArea.Height) _displayTimer.Start();
                else this.Close();
            }
        }

        public static void Show(string message, IconChar icon, Color color)
        {
            var toast = new ToastNotification(message, icon, color);
            toast.Show();
            toast._animationTimer.Start();
        }

        public static void ShowSuccess(string message) => Show(message, IconChar.CheckCircle, ThemeManager.CurrentTheme.Palette.Success);
        public static void ShowInfo(string message) => Show(message, IconChar.InfoCircle, ThemeManager.CurrentTheme.Palette.Primary);
    }
}