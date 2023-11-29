using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace WagemasterEvents.Database
{
    public class DatabaseHelper
    {
        //public static string ConnectionString => $"Data Source={AppDomain.CurrentDomain.BaseDirectory}WagemasterEvents.db;";
        public static string ConnectionString
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string databaseFolderPath = Path.Combine(appDataPath, "Wagemaster Reminders");
                // Ensure the directory exists
                Directory.CreateDirectory(databaseFolderPath);
                string databaseFilePath = Path.Combine(databaseFolderPath, "WagemasterEvents.db");
                Debug.WriteLine($"Database File Path :{databaseFilePath}" );
                return $"Data Source={databaseFilePath};";
            }
        }
        public static void InitializeDatabase()
        {
            Debug.WriteLine($"InitializeDatabase: {ConnectionString}");
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                try
                {
                    // Check if the tables exist
                    using (var command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='EventsList'", connection))
                    {
                        var tableExists = command.ExecuteScalar() != null;

                        if (!tableExists)
                        {
                            Debug.WriteLine($"Table does not exist");
                            // Create the EventsList table
                            var createTableSql = "CREATE TABLE EventsList (IDX INTEGER PRIMARY KEY AUTOINCREMENT,ID INTEGER,  Company TEXT, ReminderType TEXT, Reminder TEXT, Refno TEXT, Refname TEXT, DueDate TEXT, DatabasePath TEXT, NextReminderDate TEXT, Dismissed INTEGER, RefID INTEGER)";
                            try
                            {
                                connection.Execute(createTableSql);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error creating EventsList table: {ex.Message}");
                            }

                            //using (var createTableCommand = new SQLiteCommand(
                            //    "CREATE TABLE EventsList (Company TEXT, ReminderType TEXT, Reminder TEXT, Refno TEXT, Refname TEXT, DueDate TEXT, DatabasePath TEXT, NextReminderDate TEXT, Dismissed INTEGER)", connection))
                            //{
                            //    createTableCommand.ExecuteNonQuery();
                            // }
                        }
                        else
                        { Debug.WriteLine($"Table exists"); }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating EventsList table: {ex.Message}");
                }

                try
                {

                    using (var command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='Settings'", connection))
                    {
                        var tableExists = command.ExecuteScalar() != null;

                        if (!tableExists)
                        {
                            // Create the Settings table
                            using (var createTableCommand = new SQLiteCommand(
                                "CREATE TABLE Settings (Id INTEGER PRIMARY KEY AUTOINCREMENT, Server TEXT, [Cache Time] INTEGER, Username TEXT, Password TEXT)", connection))
                            {
                                createTableCommand.ExecuteNonQuery();

                                // Insert default values
                                using (var insertCommand = new SQLiteCommand(
                                    "INSERT INTO Settings (Server, [Cache Time], Username, Password) VALUES ('localhost', 180,'Username','Password')", connection))
                                {
                                    insertCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating Settings table: {ex.Message}");
                }

            }
        }

    }
}
