using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Text;
using YourProjectName.Models;

namespace YourProjectName.Services
{
    public interface IDatabaseService
    {
        List<Event> GetEvents();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;
        }

        public List<Event> GetEvents()
        {
            // Read the INI file to get the database paths
            List<string> databasePaths = ReadIniFile();
            List<Event> events = new List<Event>();

            // Loop through each database path and fetch event data
            foreach (string path in databasePaths)
            {
                events.AddRange(GetEventsFromDatabase(path));
            }

            return events;
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
                    // Log the path
                    _logger.LogInformation($"Log: {path}");
                    databasePaths.Add(path);
                }
            }

            return databasePaths;
        }

        private static string GetConnectionString(string databasePath)
        {
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Jet OLEDB:Database Password=!wage*master?;";
        }

        private List<Event> GetEventsFromDatabase(string databasePath)
        {
            List<Event> events = new List<Event>();
            try
            {
                string connectionString = DatabaseService.GetConnectionString(databasePath);
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT REF_NUM, REF_NAME, REMINDER_TYPE, REMINDER_MSG, REF_DATE, REMINDER_DATE, COMPANY FROM API_REMINDERS";
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Event e = new Event
                                {
                                    RefNum = reader["REF_NUM"].ToString(),
                                    RefName = reader["REF_NAME"].ToString(),
                                    ReminderType = reader["REMINDER_TYPE"].ToString(),
                                    ReminderMsg = reader["REMINDER_MSG"].ToString(),
                                    RefDate = DateTime.Parse(reader["REF_DATE"].ToString()),
                                    ReminderDate = DateTime.Parse(reader["REMINDER_DATE"].ToString()),
                                    Company = reader["COMPANY"].ToString(),
                                    DatabasePath = databasePath
                                };
                                events.Add(e);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogInformation($"Log: {ex}");
            }
            return events;
        }
    }
}

