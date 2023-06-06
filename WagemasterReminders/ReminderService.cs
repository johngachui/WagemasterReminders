using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Text;
using YourProjectName.Models;
using YourProjectName.Services;

public class ReminderService : IReminderService
{
    private ITaskService _taskService;
    private ILogger<ReminderService> _logger;
    private IDatabaseService _databaseService;
    private OleDbConnection connection;

    public ReminderService(ITaskService taskService, ILogger<ReminderService> logger, IDatabaseService databaseService, OleDbConnection connection)
    {
        _taskService = taskService;
        _logger = logger;
        _databaseService = databaseService;
        this.connection = connection;
    }
    public static class ConnectionHelper
    {
        public static string GetConnectionString(string databasePath)
        {
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Jet OLEDB:Database Password=!wage*master?;";
        }
    }
    public bool UpdateReminders(string databasePath, string username, string password)
    {
            // Check if the user has permission to perform this operation
            if (!_databaseService.GetUser(username, password, databasePath))
            {
                _logger.LogInformation($"User {username} attempted to access database {databasePath}, but does not have necessary permissions");
                return false;
            }
            else
            {
                _logger.LogInformation($"UpdateReminders started");
            }

            // Proceed with the update operation if permission granted...
            string connectionString = ConnectionHelper.GetConnectionString(databasePath);
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                //UPDATE API REMINDERS
                //1. DELETE ALL REMINDERS EXCEPT THOSE DISMISSED
                //2. UPDATE REPEAT TASKS
                //3. UPDATE TASKS
                //4. UPDATE EVERY OTHER REMINDER TYPE
                //5. UPDATE COMPANY NAME AND SHOW

                //DELETE 
                string query = $"DELETE FROM API_REMINDERS WHERE DISMISS = FALSE";
                _logger.LogInformation($"Query saved: {query}");

                try
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        // Execute the query
                        int rowsAffected = command.ExecuteNonQuery();
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error erasing API REMINDERS. Error: {ex.Message}");
                    return false;
                }

            //TASKS
            //UPDATE REPEAT TASKS

            //TaskService taskService = new TaskService(connection);
            _taskService.CreateRepeatingTasks(databasePath,connection);

            //UPDATE TASK REMINDERS
            query = @"INSERT INTO API_REMINDERS 
                 ( REF_NUM, REF_DATE, REMINDER_TYPE, REF_NAME, REMINDER_MSG, REMINDER_DATE, REF_ID, TASK_USER ) 
                 SELECT ' - ' AS Expr1, REMINDER_TASKS.DUE_DATE, 'TASK' AS TYPEX, REMINDER_TASKS.DESC, 'TASK' AS Expr3, 
                 DateAdd('d',REMINDER_TASKS.REMIND_TASK_DAYS*-1,REMINDER_TASKS.DUE_DATE) AS DATEX, REMINDER_TASKS.ID, REMINDER_TASKS.USER 
                 FROM 
                 (SELECT TASKS.ID, TASKS.DESC, TASKS.ON_HOLD, TASKS.COMPLETED, TASKS.DUE_DATE, TASKS.FIRST_REMINDER_DATE, TASKS.REMIND_TASK_DAYS, 
                 TASK_REMINDER_USERS.USER FROM Tasks Left Join TASK_REMINDER_USERS ON Tasks.ID = TASK_REMINDER_USERS.TASK 
                 WHERE NOT EXISTS 
                 (SELECT 1 FROM API_REMINDERS WHERE API_REMINDERS.REF_ID = Tasks.ID AND API_REMINDERS.REMINDER_TYPE = 'TASK') 
                 AND Tasks.COMPLETED = False AND Tasks.ON_HOLD = False AND DateDiff('d',Now(),TASKS.DUE_DATE) <= TASKS.REMIND_TASK_DAYS) AS REMINDER_TASKS 
                 WHERE 'TASK' = 'TASK';";


                try
                {
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        // Execute the query
                        int rowsAffected = command.ExecuteNonQuery();
                    }
                    _logger.LogInformation($"No error excuting query2");

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error appending tasks to API REMINDERS. Error: {ex.Message}");
                    return false;
                }


                //AFTER ALL UPDATES MARK REMINDERS AS SHOW IF REMINDERS ARE GLOBALLY ALLOWED
                string shortName = "";
                bool remindAllowed = false;
                using (OleDbCommand cmd = new OleDbCommand("SELECT SHORT_NAME, REMIND FROM COMPANY_FILES WHERE LAST = true", connection))
                {
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            shortName = reader.GetString(0);
                            remindAllowed = reader.GetBoolean(1);
                        }
                    }
                }
                string updateQuery = "";
                if (remindAllowed)
                {
                    updateQuery = "UPDATE API_REMINDERS SET COMPANY = @shortName, SHOW = true";
                }
                else
                {
                    updateQuery = "UPDATE API_REMINDERS SET COMPANY = @shortName, SHOW = false";
                }
                using (OleDbCommand updateCmd = new OleDbCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@shortName", shortName);
                    updateCmd.ExecuteNonQuery();
                }

                return true;
            }
    }


    // Your other methods...
   

}
