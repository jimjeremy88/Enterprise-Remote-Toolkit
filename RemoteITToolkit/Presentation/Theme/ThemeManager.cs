using System;
using System.Drawing;
using System.Windows.Forms;

namespace RemoteITToolkit.Presentation.Theme
{
    public static class ThemeManager
    {
        public static ITheme CurrentTheme { get; private set; } = new DarkTheme();

        public static event Action ThemeChanged;

        public static void SetTheme(ITheme theme, string accentColorName = "Sky Blue")
        {
            CurrentTheme = theme;

            // Apply custom accent color based on string setting
            switch (accentColorName)
            {
                case "Sky Blue": CurrentTheme.Palette.Primary = Color.FromArgb(2, 132, 199); break;
                case "Emerald Green": CurrentTheme.Palette.Primary = Color.FromArgb(16, 185, 129); break;
                case "Royal Purple": CurrentTheme.Palette.Primary = Color.FromArgb(139, 92, 246); break;
                case "Ruby Red": CurrentTheme.Palette.Primary = Color.FromArgb(225, 29, 72); break;
                case "Amber Orange": CurrentTheme.Palette.Primary = Color.FromArgb(245, 158, 11); break;
            }

            ThemeChanged?.Invoke();
        }

        public static void ApplyTheme(Control control)
        {
            if (control is Form form)
            {
                form.BackColor = CurrentTheme.Palette.Background;
                form.ForeColor = CurrentTheme.Palette.TextPrimary;
            }
            
            foreach (Control child in control.Controls)
            {
                ApplyTheme(child);
            }
        }
    }
}