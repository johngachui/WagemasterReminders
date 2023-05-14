using System;
using System.Diagnostics;
using System.Windows;
using WagemasterEvents.Database;

namespace WagemasterEvents
{
    public partial class MainSettings : Window
    {
        public MainSettings()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = SettingsRepository.GetSettings();
            ServerTextBox.Text = settings.Server;
            CacheTimeTextBox.Text = settings.CacheTime.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save settings to the database here
            var server = ServerTextBox.Text;
            var cacheTime = int.Parse(CacheTimeTextBox.Text);

            Debug.WriteLine($"Saving settings: server={server}, cacheTime={cacheTime}");

            SettingsRepository.UpdateSettings(server, cacheTime);
            MessageBox.Show("Settings saved successfully.");
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
