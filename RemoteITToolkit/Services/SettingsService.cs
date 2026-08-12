using System;
using System.IO;
using System.Linq;
using RemoteITToolkit.Core.Entities;
using RemoteITToolkit.Core.Interfaces;

namespace RemoteITToolkit.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly string _dbPath;

        public SettingsService(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "RemoteIT.db");
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            var setting = _settingsRepository.GetAll().FirstOrDefault(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        public void SaveSetting(string key, string value)
        {
            var existing = _settingsRepository.GetAll().FirstOrDefault(s => s.Key == key);
            if (existing != null) { existing.Value = value; _settingsRepository.Update(existing); }
            else { _settingsRepository.Add(new Setting { Key = key, Value = value }); }
        }

        public string Theme { get => GetSetting("Theme", "Dark"); set => SaveSetting("Theme", value); }
        public string AccentColor { get => GetSetting("AccentColor", "Sky Blue"); set => SaveSetting("AccentColor", value); }
        public string FontSize { get => GetSetting("FontSize", "Medium"); set => SaveSetting("FontSize", value); }
        public bool AutoRefresh { get => bool.TryParse(GetSetting("AutoRefresh", "True"), out bool val) ? val : true; set => SaveSetting("AutoRefresh", value.ToString()); }
        public int RefreshInterval { get => int.TryParse(GetSetting("RefreshInterval", "10000"), out int val) ? val : 10000; set => SaveSetting("RefreshInterval", value.ToString()); }
        public string Language { get => GetSetting("Language", "English"); set => SaveSetting("Language", value); }
        public string ExportFolder { get => GetSetting("ExportFolder", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "IT_Reports")); set => SaveSetting("ExportFolder", value); }

        public void BackupDatabase(string destinationPath)
        {
            if (File.Exists(_dbPath)) File.Copy(_dbPath, destinationPath, true);
            else throw new FileNotFoundException("Cannot find the active database file to backup.");
        }

        public void RestoreDatabase(string sourcePath)
        {
            if (File.Exists(sourcePath)) File.Copy(sourcePath, _dbPath, true);
            else throw new FileNotFoundException("The selected backup file does not exist.");
        }

        public void ResetSettingsToDefault()
        {
            Theme = "Dark"; AccentColor = "Sky Blue"; FontSize = "Medium"; AutoRefresh = true; RefreshInterval = 10000; Language = "English";
            ExportFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "IT_Reports");
        }
    }
}