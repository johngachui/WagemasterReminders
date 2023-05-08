using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using Dapper;
using WagemasterEvents.Models;

namespace WagemasterEvents.Database
{
    public static class SettingsRepository
    {
        public static Settings GetSettings()
        {
            using (IDbConnection connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                var settings = connection.QueryFirstOrDefault<Settings>("SELECT * FROM Settings LIMIT 1");
                if (settings == null)
                {
                    settings = new Settings
                    {
                        Server = "localhost",
                        CacheTime = 300
                    };
                }
                else
                {
                    if (settings.Server == null)
                    {
                        settings.Server = "localhost";
                    }

                    if (settings.CacheTime < 1)
                    {
                        settings.CacheTime = 300;
                    }
                }

                return settings;
            }
        }

        public static void UpdateSettings(string server, int cacheTime)
        {
            using (IDbConnection connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                Debug.WriteLine($"Connection state is {connection.State}");
                string sql = "UPDATE Settings SET Server = @Server, [Cache Time] = @CacheTime";
                Debug.WriteLine($"Executing SQL query: {sql}");
                connection.Execute(sql, new { Server = server, CacheTime = cacheTime });
            }
        }
    }
}
