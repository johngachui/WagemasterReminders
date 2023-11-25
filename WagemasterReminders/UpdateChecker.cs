﻿using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace YourProjectName.Models
{
    public class UpdateChecker
    {
        private const string VersionCheckUrl = "https://digitalframeworksltd.com/WagemasterAPI/version.json";
        private readonly string currentVersion;

        public UpdateChecker(string currentVersion)
        {
            this.currentVersion = "1.0.1";
        }

        public async Task CheckForUpdatesAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync(VersionCheckUrl);
                var updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(response);

                if (updateInfo != null && IsNewVersionAvailable(updateInfo.Version))
                {
                    string? latestVersion = updateInfo.Version;
                    string? downloadUrl = updateInfo.Url;
                    string? checksum = updateInfo.Checksum;

                    // Logic to handle the available update
                    Console.WriteLine("New version available: " + latestVersion);
                    if (!string.IsNullOrEmpty(downloadUrl) && !string.IsNullOrEmpty(checksum))
                    {
                        // Call DownloadUpdateAsync here
                        bool updateDownloaded = await DownloadUpdateAsync(downloadUrl, checksum);
                        if (updateDownloaded)
                        {
                            // Handle successful download and prompt for installation
                            MessageBox.Show("Update completed successfully", "Update Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            // Handle failed download
                            Console.WriteLine("Failed to download the update.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Update information is incomplete.");
                    }
                }
                else
                {
                    Console.WriteLine("No updates available.");
                }
            }
        }

        private bool IsNewVersionAvailable(string? latestVersion)
        {
            return !string.Equals(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase);
        }

        private class UpdateInfo
        {
            public string? Version { get; set; }
            public string? Url { get; set; }
            public string? Checksum { get; set; } // Make sure to include the checksum in your version.json
        }

        private static bool VerifyFileChecksum(string filePath, string expectedChecksum)
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
        public async Task <bool> DownloadUpdateAsync(string downloadUrl, string expectedChecksum)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error downloading the update.");
                }

                var filePath = Path.Combine(Path.GetTempPath(), "new_setup.exe");
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }

                if (VerifyFileChecksum(filePath, expectedChecksum))
                {
                    // Proceed with the update
                    DialogResult dialogResult = MessageBox.Show("A new version is available. Do you want to update now?", "Update Available", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        // Proceed with update
                        // Save any unsaved work here
                        Application.Exit();
                        Process.Start(filePath);
                        //Check if update was successful
                        string indicatorFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update_indicator.txt");
                        if (File.Exists(indicatorFilePath))
                        {
                            // Update was successful
                            File.Delete(indicatorFilePath); // Clean up the indicator file
                            File.Delete(filePath); // Delete the temporary installer file                                // Additional post-update logic
                        }
                        Process.Start("WagemasterAPI.exe");
                    }

                    return true;
                }
                else
                {
                    // Handle checksum mismatch
                    return false;
                }
                // StartUpdateProcess(filePath);
            }

        }

    }
}