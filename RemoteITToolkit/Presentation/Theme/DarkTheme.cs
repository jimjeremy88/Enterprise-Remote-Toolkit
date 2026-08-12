using System.Drawing;

namespace RemoteITToolkit.Presentation.Theme
{
    public class DarkTheme : ITheme
    {
        public string Name => "Dark";
        public ColorPalette Palette => new ColorPalette
        {
            Background = Color.FromArgb(15, 23, 42),
            Surface = Color.FromArgb(30, 41, 59),
            Primary = Color.FromArgb(56, 189, 248),
            Success = Color.FromArgb(34, 197, 94),
            Warning = Color.FromArgb(245, 158, 11),
            Error = Color.FromArgb(239, 68, 68),
            TextPrimary = Color.FromArgb(248, 250, 252),
            TextSecondary = Color.FromArgb(148, 163, 184),
            Border = Color.FromArgb(51, 65, 85),
            SidebarBackground = Color.FromArgb(2, 6, 23),
            SidebarActive = Color.FromArgb(30, 41, 59)
        };
    }
}