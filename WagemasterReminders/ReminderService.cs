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
    

    public ReminderService(ITaskService taskService, ILogger<ReminderService> logger, IDatabaseService databaseService)
    {
        _taskService = taskService;
        _logger = logger;
        _databaseService = databaseService;
       
    }
    public static class ConnectionHelper
    {
        public static string GetConnectionString(string databasePath)
        {
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Jet OLEDB:Database Password=!wage*master?;";
        }
    }
    public List<Event> UpdateReminders(string username, string password)
    {
        // Read the INI file to get the database paths
        List<string> databasePaths = _databaseService.ReadIniFile();
        List<Event> events = new List<Event>();

        // Loop through each database path and fetch event data
        foreach (string path in databasePaths)
        {
            if (!_databaseService.GetUser(username, password, path))
            {
                _logger.LogInformation($"path1 = *******************"); //SHOW ROWCOUNT
                bool updated = GetLatestReminders(path, username, password);
                _logger.LogInformation($"path2 = Updated Reminders = {updated}"); //SHOW ROWCOUNT 
            }
        }
        return events;
    }
    private bool GetLatestReminders(string databasePath, string username, string password)
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
        //UPDATE REPEAT TASKS
        _taskService.CreateRepeatingTasks(databasePath, connectionString);

        using (OleDbConnection connection = new OleDbConnection(connectionString))
        {
            connection.Open();

            //UPDATE API REMINDERS
            //1. DELETE ALL REMINDERS EXCEPT THOSE DISMISSED
            //2. UPDATE REPEAT TASKS
            //3. UPDATE TASKS
            //4. UPDATE EVERY OTHER REMINDER TYPE
            //5. UPDATE COMPANY NAME AND SHOW

            //DELETE 
            string query = $"DELETE FROM API_REMINDERS WHERE DISMISS = FALSE AND (REMINDER_TYPE = 'ANNUAL' OR REMINDER_TYPE = 'MATERNITY' OR REMINDER_TYPE = 'PATERNITY' OR  REMINDER_TYPE = 'DOCUMENT') ";
            _logger.LogInformation($"Query saved: {query}");

            try
            {
                
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

            //UNAPPROVED LEAVE
            // Query to retrieve the value REMIND_UNAPPROVED_LEAVE_DAYS
            string systemQuery = "SELECT REMIND_UNAPPROVED_LEAVE_DAYS FROM COMPANY_FILES WHERE LAST = TRUE";
            long remindUnapprovedLeaveDays=0;

            using (OleDbCommand cmd = new OleDbCommand(systemQuery, connection))
            {
                try
                {
                    remindUnapprovedLeaveDays = (Int16)cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    // Handle the error
                    _logger.LogInformation("Error retrieving REMIND_UNAPPROVED_LEAVE_DAYS: " + ex.Message);
                }
            }

            string insertQuery = $@"
            INSERT INTO API_REMINDERS (REF_NUM, REF_DATE, REMINDER_TYPE, REF_NAME, REMINDER_MSG, REMINDER_DATE)
            SELECT L.NUM, L.START, L.TYPE, M.NAME, L.TYPE & ' LEAVE APPROVAL DUE' AS MSG, DateAdd('d', {remindUnapprovedLeaveDays} * -1, L.START) As DATEX
            FROM [LEAVE DAYS TAKEN] AS L
            INNER JOIN [HR_MASTER] AS M ON L.NUM = M.NUM
            WHERE (DateDiff('d', Now(), L.START) >= -365 AND L.TYPE IN ('ANNUAL', 'MATERNITY', 'PATERNITY') AND L.TAKEN = False AND L.APPROVED = False AND L.NOT_APPROVED = False AND M.EMPLOYED = True)
            AND NOT EXISTS (
                SELECT 1
                FROM API_REMINDERS
                WHERE API_REMINDERS.REF_NUM = L.NUM
                AND API_REMINDERS.REF_DATE = L.START
                AND API_REMINDERS.REMINDER_TYPE IN ('ANNUAL', 'MATERNITY', 'PATERNITY')
            )
            AND DateDiff('d', Now(), L.START) <= {remindUnapprovedLeaveDays}";

            using (OleDbCommand insertCmd = new OleDbCommand(insertQuery, connection))
            {
                try
                {
                    insertCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    // Handle the error
                    _logger.LogInformation("Error executing UNAPPROVED LEAVE insert query: " + ex.Message);
                }
            }

            //UPDATE DOCUMENTS
            string typeX = "DOCUMENT";

            // Insert statement
            insertQuery = $@"
            INSERT INTO API_REMINDERS (REF_NUM, REF_DATE, REMINDER_TYPE, REF_NAME, REMINDER_MSG, REMINDER_DATE)
            SELECT IIf(IsNull(D.NUM)=False,D.NUM,'-') AS NUMX, D.EXPIRY_DATE, '{typeX}' AS TYPEX, IIf(IsNull(HR.NUM)=False,HR.NAME,'ADMINISTRATOR') AS Expr1, Left(D.SHORT_DESC,28) & ' EXPIRING' AS MSG, D.REMINDER_DATE AS DATEX
            FROM DOCUMENTS AS D LEFT JOIN HR_MASTER AS HR ON HR.NUM = D.NUM
            WHERE (DateDiff('d', Now(), D.REMINDER_DATE) <= 0 AND ((HR.EMPLOYED = True AND HR.NUM Is Not Null) OR HR.NUM Is Null))
            AND NOT EXISTS (
                SELECT 1
                FROM API_REMINDERS
                WHERE API_REMINDERS.REF_NUM = D.NUM
                AND API_REMINDERS.REF_DATE = D.REMINDER_DATE
                AND API_REMINDERS.REMINDER_TYPE = '{typeX}'
            )";

            using (OleDbCommand insertCmd = new OleDbCommand(insertQuery, connection))
            {
                try
                {
                    insertCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    // Handle the error
                    _logger.LogInformation("Error executing DOCUMENT insert query: " + ex.Message);
                }
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
