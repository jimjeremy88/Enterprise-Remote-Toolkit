using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteITToolkit.Presentation.Theme;

namespace RemoteITToolkit.Presentation.Controls
{
    public class SidebarButton : Button
    {
        public SidebarButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.Height = 50;
            this.Dock = DockStyle.Top;
            this.TextAlign = ContentAlignment.MiddleLeft;
            this.Padding = new Padding(15, 0, 0, 0);
            this.Cursor = Cursors.Hand;

            ApplyTheme();
            ThemeManager.ThemeChanged += ApplyTheme;
        }

        private void ApplyTheme()
        {
            this.BackColor = ThemeManager.CurrentTheme.Palette.SidebarBackground;
            this.ForeColor = ThemeManager.CurrentTheme.Palette.TextPrimary;

            // FIXED: Uses the new SidebarActive property
            this.FlatAppearance.MouseOverBackColor = ThemeManager.CurrentTheme.Palette.SidebarActive;
            this.FlatAppearance.MouseDownBackColor = ThemeManager.CurrentTheme.Palette.Primary;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= ApplyTheme;
            base.Dispose(disposing);
        }
    }
}