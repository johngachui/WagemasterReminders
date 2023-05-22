using System;
using System.Diagnostics;
using System.Windows;
using WagemasterEvents.Database;

namespace WagemasterEvents
{

    public partial class MainSettings : Window
    {
        public delegate void CacheTimeChangedEventHandler(int newCacheTime);
        public event CacheTimeChangedEventHandler CacheTimeChanged;

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
            UsernameTextBox.Text = settings.Username;
            PasswordTextBox.Password = settings.Password;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(CacheTimeTextBox.Text, out _))  //If the input cannot be parsed into an integer
            {
                // Save settings to the database here
                var server = ServerTextBox.Text;
                var cacheTime = int.Parse(CacheTimeTextBox.Text);
                var username = !string.IsNullOrEmpty(UsernameTextBox.Text) ? UsernameTextBox.Text : "Username";
                var password = !string.IsNullOrEmpty(PasswordTextBox.Password) ? PasswordTextBox.Password : "Password";

                Debug.WriteLine($"Saving settings: server={server}, cacheTime={cacheTime}");

                SettingsRepository.UpdateSettings(server, cacheTime,username,password);
                CacheTimeChanged?.Invoke(int.Parse(CacheTimeTextBox.Text));
                MessageBox.Show("Settings saved successfully.");
                Close();
            }
            else
            {
                MessageBox.Show("Invalid reminder interval");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
