using Microsoft.Data.Sqlite;
using System;

namespace WagemasterEvents.Database
{
    public class DatabaseHelper
    {
        public static string ConnectionString => $"Data Source={AppDomain.CurrentDomain.BaseDirectory}WagemasterEvents.db;";

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                string eventsListTable = "CREATE TABLE IF NOT EXISTS EventsList (" +
                    "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    "Company TEXT, " +
                    "ReminderType TEXT, " +
                    "Reminder TEXT, " +
                    "DueDate DATE, " +
                    "NextReminderDate DATE, " +
                    "Dismissed BIT, " +
                    "Refno TEXT, " +
                    "DatabasePath TEXT, " +
                    "Refname TEXT);";

                string settingsTable = "CREATE TABLE IF NOT EXISTS Settings (" +
                    "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    "Server TEXT, " +
                    "CacheTime INTEGER);";

                using (var command = new SqliteCommand(eventsListTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(settingsTable, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
