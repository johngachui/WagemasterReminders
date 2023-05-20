using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.ApplicationServices;
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
        List<Event> GetEvents(string username, string password);
        bool GetUser(string username, string password, string databasePath);

        bool UpdateEvent(int id, bool dismissed, string username, string databasePath, string password);
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;
        }

        public bool GetUser(string username, string password, string databasePath)

        {
            string connectionString = GetConnectionString(databasePath);
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                // Check if the PASSWORDS table is empty
                string countQuery = "SELECT COUNT(*) FROM PASSWORDS";
                using (OleDbCommand countCommand = new OleDbCommand(countQuery, connection))
                {
                    int rowCount = (int)countCommand.ExecuteScalar();
                    _logger.LogInformation($"rowCount = {rowCount}"); //SHOW ROWCOUNT                                        
                    if (rowCount == 0)
                    {
                        _logger.LogInformation($"return1 = true"); //SHOW ROWCOUNT 
                        return true;  // Return true if the PASSWORDS table is empty
                    }
                }

                // Query to find a user with the provided username and SHOW_REMINDERS permission
                string userQuery = "SELECT [SHOW_REMINDERS] FROM PASSWORDS WHERE [USER NAME]=@UserName AND [PASSWORD]=@Password";
                using (OleDbCommand userCommand = new OleDbCommand(userQuery, connection))
                {
                    userCommand.Parameters.AddWithValue("@UserName", username);
                    userCommand.Parameters.AddWithValue("@Password", password);

                    object result = userCommand.ExecuteScalar();
                    if (result != null)
                    {
                        return Convert.ToBoolean(result);
                    }
                }
            }

            return false;  // Return false if user was not found or if user does not have SHOW_REMINDERS permission
        }


        public bool UpdateEvent(int id, bool dismissed, string databasePath, string username, string password)
        {
            // Check if the user has permission to perform this operation
            if (!GetUser(username, password, databasePath))
            {
                _logger.LogInformation($"User {username} attempted to update event with id {id} in database {databasePath}, but does not have necessary permissions");
                return false;
            }

            // Proceed with the update operation if permission granted...
            string connectionString = GetConnectionString(databasePath);
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = $"UPDATE API_REMINDERS SET DISMISS = ? WHERE ID = ?";

                try
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        // Add parameters to prevent SQL Injection
                        command.Parameters.Add(new OleDbParameter("dismissed", dismissed));
                        command.Parameters.Add(new OleDbParameter("id", id));

                        // Execute the query
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            _logger.LogInformation($"No rows were updated. Event with id {id} might not exist in database {databasePath}");
                            return false;
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error updating event with id {id} in database {databasePath}. Error: {ex.Message}");
                    return false;
                }
            }
        }



        public List<Event> GetEvents(string username, string password)
        {
            // Read the INI file to get the database paths
            List<string> databasePaths = ReadIniFile();
            List<Event> events = new List<Event>();

            // Loop through each database path and fetch event data
            foreach (string path in databasePaths)
            {
                if (GetUser(username, password, path))
                {
                    _logger.LogInformation($"path1 = {path}"); //SHOW ROWCOUNT
                    events.AddRange(GetEventsFromDatabase(path));
                     
                }
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
                    _logger.LogInformation($"Log1: {path}");
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

                    string query = "SELECT ID, REF_NUM, REF_NAME, REMINDER_TYPE, REMINDER_MSG, REF_DATE, REMINDER_DATE, COMPANY, DISMISS FROM API_REMINDERS";
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            try
                            {
                                _logger.LogInformation($"X1");
                                while (reader.Read())
                                {

                                    Event e = new Event
                                    {
                                        ID = (int)reader["ID"],
                                        RefNum = reader["REF_NUM"].ToString(),
                                        RefName = reader["REF_NAME"].ToString(),
                                        ReminderType = reader["REMINDER_TYPE"].ToString(),
                                        ReminderMsg = reader["REMINDER_MSG"].ToString(),
                                        RefDate = DateTime.Parse(reader["REF_DATE"].ToString()),
                                        ReminderDate = DateTime.Parse(reader["REMINDER_DATE"].ToString()),
                                        Company = reader["COMPANY"].ToString(),
                                        DatabasePath = databasePath,
                                        Dismissed = (bool)reader["DISMISS"]
                                    };
                                    events.Add(e);
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                _logger.LogError(ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogInformation($"Log2: {ex}");
            }
            return events;
        }
    }
}

