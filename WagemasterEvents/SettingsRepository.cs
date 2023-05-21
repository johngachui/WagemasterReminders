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
                var settings = connection.QueryFirstOrDefault<Settings>("SELECT Server , [Cache Time] as Cachetime, Username, Password FROM Settings LIMIT 1");
                if (settings == null)
                {
                    settings = new Settings
                    {
                        Server = "localhost",
                        CacheTime = 180,
                        Username = "Username",
                        Password = "Password"
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
                        settings.CacheTime = 180;
                    }

                    if (settings.Username == null)
                    {
                        settings.Username = "Username";
                    }

                    if (settings.Password == null)
                    {
                        settings.Password = "Password";
                    }

                }

                return settings;
            }
        }

        public static void UpdateSettings(string server, int cacheTime, string username, string password)
        {
            using (IDbConnection connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                Debug.WriteLine($"Connection state is {connection.State}");
                string sqlSelect = "SELECT * FROM Settings LIMIT 1";
                Debug.WriteLine($"Executing SQL query: {sqlSelect}");
                var existingSettings = connection.QueryFirstOrDefault<Settings>(sqlSelect);

                if (existingSettings == null)
                {
                    string sqlInsert = "INSERT INTO Settings (Server, [Cache Time], Username, Password) VALUES (@Server, @CacheTime, @Username, @Password)";
                    Debug.WriteLine($"Executing SQL query: {sqlInsert} with values {server} and {cacheTime}");
                    try
                    {
                        connection.Execute(sqlInsert, new { Server = server, CacheTime = cacheTime, Username = username, Password = password});
                    }
                    catch (SQLiteException e)
                    {
                        Debug.WriteLine($"SQLite Error {e.Message}");
                    }
                }
                else
                {
                    string sqlUpdate = "UPDATE Settings SET Server = @Server, [Cache Time] = @CacheTime, Username = @Username, Password = @Password WHERE Id = @Id";
                    Debug.WriteLine($"Executing SQL query: {sqlUpdate}");
                    try
                    {
                        connection.Execute(sqlUpdate, new { Server = server, CacheTime = cacheTime, Username = username,Password =password, Id = existingSettings.Id });
                    }
                    catch (SQLiteException e)
                    {
                        Debug.WriteLine($"SQLite Error {e.Message}");
                    }
                }
            }
        }


    }
}
