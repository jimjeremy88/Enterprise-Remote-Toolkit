using System.Drawing;

namespace RemoteITToolkit.Presentation.Theme
{
    public class LightTheme : ITheme
    {
        public string Name => "Light";
        public ColorPalette Palette => new ColorPalette
        {
            Background = Color.FromArgb(241, 245, 249),
            Surface = Color.FromArgb(255, 255, 255),
            Primary = Color.FromArgb(2, 132, 199),
            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(217, 119, 6),
            Error = Color.FromArgb(220, 38, 38),
            TextPrimary = Color.FromArgb(15, 23, 42),
            TextSecondary = Color.FromArgb(100, 116, 139),
            Border = Color.FromArgb(226, 232, 240),
            SidebarBackground = Color.FromArgb(2, 6, 23),
            SidebarActive = Color.FromArgb(30, 41, 59)
        };
    }
}