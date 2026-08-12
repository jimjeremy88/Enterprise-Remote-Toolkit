using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RemoteITToolkit.Presentation.Controls
{
    public class RoundedButton : Button
    {
        public int BorderRadius { get; set; } = 10;
        private bool _isHovered = false;

        public RoundedButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.DodgerBlue;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, this.Width, this.Height);
            var path = GetRoundedPath(rect, BorderRadius);

            this.Region = new Region(path);

            Color bgColor = _isHovered ? ControlPaint.Light(this.BackColor) : this.BackColor;

            using (var brush = new SolidBrush(bgColor))
            {
                pevent.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font, rect, this.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2F;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Width - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Width - d, rect.Height - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}