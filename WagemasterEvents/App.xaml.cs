using System.Windows;
using System.Windows.Forms;
using WagemasterEvents.Database;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using static WagemasterEvents.App;

namespace WagemasterEvents
{
    public partial class App : System.Windows.Application
    {
        private NotifyIcon? notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = new MainWindow();
            MainWindow.Hide();

            notifyIcon = new NotifyIcon();
            notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            notifyIcon.Icon = new System.Drawing.Icon("icon.ico");
            notifyIcon.Visible = true;
            notifyIcon.Text = "Wagemaster Reminders v 1.0.0";

            CreateContextMenu();

            LoadEventsFromApi();
            //await CheckForUpdatesAsync();
            // Call the asynchronous method without awaiting it
            CheckForUpdatesAsync().ContinueWith(task =>
            {
               if (task.Exception != null)
                {
                    // Handle exceptions
                    // You can log this exception or show a message to the user
                    Debug.WriteLine($"CheckForUpdatesAsync : {task.Exception}");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var currentVersion = new Version("1.0.0"); // Current version of the app
                UpdateInfo? updateInfo = null;
                try
                {
                    updateInfo = await GetUpdateInfoAsync(); // Implement this method to get update info from your server
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"JSON Error2: {ex.Message}");
                }
                if (updateInfo != null)
                {
                    if (updateInfo.Version > currentVersion)
                    {
                        Debug.WriteLine($"updateInfo.Version : {updateInfo.Version} currentVersion: {currentVersion}");

                        var result = System.Windows.MessageBox.Show("Update available. Do you want to update now?", "Wagemaster Reminders - Update Available", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            await UpdateApplication(updateInfo); // Implement this method to download and start the update installer
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"task.Exception : {ex.Message}");
            }
        }

        public class UpdateInfo
        {
            public Version? Version { get; set; }
            public string? Url { get; set; }
            public string? Checksum { get; set; }
        }

        private async Task<UpdateInfo?> GetUpdateInfoAsync()
        {
            using (var client = new HttpClient())
            {
                string url = "https://digitalframeworksltd.com/WagemasterReminders/versionReminders.json";

                try
                {
                    var response = await client.GetStringAsync(url);
                    return JsonConvert.DeserializeObject<UpdateInfo>(response);
                }
                catch (Exception ex) 
                { 
                    Debug.WriteLine($"JSON Error: {ex.Message}");
                    return null;
                }
                
            }
        }

        private async Task UpdateApplication(UpdateInfo updateInfo)
        {
            string tempPath = Path.GetTempPath();
            string installerPath = Path.Combine(tempPath, "WagemasterEventsUpdate.exe");
            string? downloadURL = updateInfo.Url;

            Debug.WriteLine($"updateInfo : {downloadURL}");

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(downloadURL, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error downloading the update.: {downloadURL}");
                    throw new Exception("Error downloading the update.");
                }
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }

            // Verify checksum
            if (VerifyChecksum(installerPath, updateInfo.Checksum))
            {
                Process.Start(installerPath);

                // Close the current application
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                // Handle invalid checksum: notify user, log error, etc.
                System.Windows.MessageBox.Show("Incomplete or corrupted download, try again!", "Wagemaster Reminders - File Download Failure", MessageBoxButton.OK);
            }
        }

        /*private bool VerifyChecksum(string filePath, string? expectedChecksum)
        {
            if (expectedChecksum != null)
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hash = sha256.ComputeHash(stream);
                        string fileChecksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        return fileChecksum == expectedChecksum.ToLowerInvariant();
                    }
                }
            }
            else
            {
                return false;
            }
        }*/
        private static bool VerifyChecksum(string filePath, string? expectedChecksum)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var fileChecksum = sha256.ComputeHash(stream);
                    var fileChecksumString = BitConverter.ToString(fileChecksum).Replace("-", String.Empty);
                    return fileChecksumString.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        
        private async void LoadEventsFromApi()
        {
            try
            {
                var apiHelper = new ApiHelper();
                var server = SettingsRepository.GetSettings().Server;
                var username = SettingsRepository.GetSettings().Username;
                var password = SettingsRepository.GetSettings().Password;
                
                var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server, username, password);
                EventsRepository.SaveEvents(fetchedEvents);
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"LoadEventsFromApi error : {ex.Message}"); 
            }
        }

        private void CreateContextMenu()
        {
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Show").Click += (s, e) => ShowMainWindow();
            notifyIcon.ContextMenuStrip.Items.Add("Check for Updates").Click += CheckForUpdates_Click;
            notifyIcon.ContextMenuStrip.Items.Add("Exit").Click += (s, e) => ExitApplication();
        }

        private async void CheckForUpdates_Click(object? sender, EventArgs e)
        {
            // Implement the update check logic here
            var updateInfo = await GetUpdateInfoAsync();
            var currentVersion = new Version("1.0.0"); // current version number
            if (updateInfo != null && updateInfo.Version > currentVersion) //System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)
            {
                // New update available
                var result = System.Windows.MessageBox.Show("An update is available. Would you like to update now?", "Wagemaster Reminders - Update Available", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    await UpdateApplication(updateInfo);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("You are using the latest version.", "Wagemaster Reminders - No Updates Found");
            }
        }
        private void ShowMainWindow()
        {
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
        }

        private void ExitApplication()
        {
            notifyIcon.Visible = false;
            Shutdown();
        }
    }
}
