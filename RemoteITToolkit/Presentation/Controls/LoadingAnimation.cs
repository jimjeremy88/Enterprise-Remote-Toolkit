using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RemoteITToolkit.Presentation.Controls
{
    public class LoadingAnimation : Control
    {
        private readonly Timer _timer;
        private int _angle = 0;

        public Color SpinnerColor { get; set; } = Color.DodgerBlue;

        public LoadingAnimation()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = Color.Transparent;
            this.Size = new Size(40, 40);

            _timer = new Timer { Interval = 30 };
            _timer.Tick += (s, e) =>
            {
                _angle = (_angle + 15) % 360;
                this.Invalidate();
            };
        }

        public void Start() { this.Visible = true; _timer.Start(); }
        public void Stop() { _timer.Stop(); this.Visible = false; }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float penWidth = 4f; // Slightly thicker for 200% resolution
            using (var pen = new Pen(SpinnerColor, penWidth) { DashStyle = DashStyle.Custom, DashPattern = new float[] { 4, 4 } })
            {
                float cx = this.Width / 2f;
                float cy = this.Height / 2f;

                // FIXED: Massive 6-pixel safety buffer ensures it never touches the bounds
                float radius = Math.Min(cx, cy) - (penWidth / 2f) - 6f;

                e.Graphics.TranslateTransform(cx, cy);
                e.Graphics.RotateTransform(_angle);

                if (radius > 0)
                {
                    e.Graphics.DrawArc(pen, -radius, -radius, radius * 2, radius * 2, 0, 360);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}