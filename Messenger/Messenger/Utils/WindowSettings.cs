using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Messenger.Utils
{
    public class WindowSettings
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public WindowState WindowState { get; set; }

        public static WindowSettings Load()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var settingsFile = Path.Combine(appData, "Messenger", "window_settings.json");

                if (File.Exists(settingsFile))
                {
                    var json = File.ReadAllText(settingsFile);
                    return JsonSerializer.Deserialize<WindowSettings>(json) ?? new WindowSettings();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "LoadWindowSettings");
            }

            return new WindowSettings();
        }

        public static void Save(WindowSettings settings)
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appData, "Messenger");

                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                var settingsFile = Path.Combine(appFolder, "window_settings.json");
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFile, json);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogException(ex, "SaveWindowSettings");
            }
        }
    }
}