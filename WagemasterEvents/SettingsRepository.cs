using System.Data;
using System.Data.SQLite;
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
                return settings;
            }
        }

        public static void UpdateSettings(string server, int cacheTime)
        {
            using (IDbConnection connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Execute("UPDATE Settings SET Server = @Server, CacheTime = @CacheTime", new { Server = server, CacheTime = cacheTime });
            }
        }
    }
}
