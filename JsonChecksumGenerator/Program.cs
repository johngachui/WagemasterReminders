using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace JsonChecksumGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: JsonChecksumGenerator <Base URL> <Setup File Name> <Version>");
                return;
            }

            string baseUrl = args[0];
            string setupFileName = args[1];
            string version = args[2];

            try
            {
                string checksum = ComputeChecksum(setupFileName);
                string json = CreateJson(version, baseUrl + setupFileName, checksum);

                File.WriteAllText("version.json", json);
                Console.WriteLine("version.json created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private static string ComputeChecksum(string fileName)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    byte[] checksum = sha256.ComputeHash(stream);
                    return BitConverter.ToString(checksum).Replace("-", String.Empty);
                }
            }
        }

        private static string CreateJson(string version, string url, string checksum)
        {
            var data = new
            {
                version,
                url,
                checksum
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}

