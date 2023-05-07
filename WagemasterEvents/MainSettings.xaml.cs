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
            SettingsRepository.UpdateSettings(ServerTextBox.Text, int.Parse(CacheTimeTextBox.Text));
            MessageBox.Show("Settings saved successfully.");
            Close();
        }
    }
}
