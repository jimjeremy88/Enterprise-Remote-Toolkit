namespace RemoteITToolkit.Core.Interfaces
{
    public interface ISettingsService
    {
        string GetSetting(string key, string defaultValue = "");
        void SaveSetting(string key, string value);

        string Theme { get; set; }
        string AccentColor { get; set; }
        string FontSize { get; set; }
        bool AutoRefresh { get; set; }
        int RefreshInterval { get; set; }
        string Language { get; set; }
        string ExportFolder { get; set; }

        void BackupDatabase(string destinationPath);
        void RestoreDatabase(string sourcePath);
        void ResetSettingsToDefault();
    }
}