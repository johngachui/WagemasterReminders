using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Data.SQLite;
using System.Diagnostics;

namespace WagemasterEvents.Database
{
    public class DatabaseHelper
    {
        public static string ConnectionString => $"Data Source={AppDomain.CurrentDomain.BaseDirectory}WagemasterEvents.db;";

        public static void InitializeDatabase()
        {
            //Debug.WriteLine($"Database file path: {ConnectionString}");
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // Check if the tables exist
                using (var command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='EventsList'", connection))
                {
                    var tableExists = command.ExecuteScalar() != null;

                    if (!tableExists)
                    {
                        //Debug.WriteLine($"Table  does not exist");
                        // Create the EventsList table
                        var createTableSql = "CREATE TABLE EventsList (IDX INTEGER PRIMARY KEY AUTOINCREMENT,ID INTEGER,  Company TEXT, ReminderType TEXT, Reminder TEXT, Refno TEXT, Refname TEXT, DueDate TEXT, DatabasePath TEXT, NextReminderDate TEXT, Dismissed INTEGER)";
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
                }

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
        }

    }
}
