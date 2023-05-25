using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using WagemasterEvents.Database;
using System.IO;

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
            ServerComboBox.ItemsSource = GetServerNamesFromDatabasePaths(ReadIniFile());
            ServerComboBox.Text = settings.Server;
            CacheTimeTextBox.Text = settings.CacheTime.ToString();
            UsernameTextBox.Text = settings.Username;
            PasswordTextBox.Password = settings.Password;
        }

        private List<string> GetServerNamesFromDatabasePaths(List<string> databasePaths)
        {
            List<string> serverNames = new List<string> { "localhost" };

            foreach (var path in databasePaths)
            {
                if (path.StartsWith("\\\\"))
                {
                    var serverName = path.Split('\\')[2];
                    if (!serverNames.Contains(serverName))
                        serverNames.Add(serverName);
                }
                else
                {
                    if (!serverNames.Contains("localhost"))
                        serverNames.Add("localhost");
                }
            }

            return serverNames;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(CacheTimeTextBox.Text, out _))  //If the input cannot be parsed into an integer
            {
                // Save settings to the database here
                var server = ServerComboBox.Text;
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

        private List<string> ReadIniFile()
        {
            // Construct the INI file path
            string iniPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WagemasterPayroll",
                "WagemasterPayroll.ini"
            );

            // Read the contents of the INI file
            string[] lines = File.ReadAllLines(iniPath, Encoding.Default);
            List<string> databasePaths = new List<string>();

            // Loop through each line and extract the database paths
            foreach (string line in lines)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string path = line.Substring(1, line.Length - 2);
                    path = Path.Combine(path, "Wagemaster_data.mdb");

                    databasePaths.Add(path);
                }
            }

            return databasePaths;
        }


    }
}
