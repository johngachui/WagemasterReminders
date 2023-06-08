using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using YourProjectName.Models;

namespace YourProjectName.Services
{
    public class TaskService : ITaskService
    {
        private ILogger<TaskService> _logger;

        public TaskService(ILogger<TaskService> logger)
        {
           _logger = logger;
        }
        public void CreateRepeatingTasks(string databasePath, string connectionstr)
        {
            _logger.LogInformation($"CreateRepeatingTasks started");
            using (OleDbConnection connection = new OleDbConnection(connectionstr))
            {
                DateTime todaysDate = DateTime.Today;
                
                connection.Open();
                _logger.LogInformation($"CreateRepeatingTasks connection open ");
                string sqlSelectTasks = @"
                SELECT * 
                FROM TASKS 
                WHERE [REPEAT] = TRUE 
                AND ([REPEAT_STOP_DATE] > @todaysDate OR ISDATE([REPEAT_STOP_DATE]) = 0) 
                AND [CHILD_OF_TASK_ID] IS NULL";

                using (OleDbCommand selectCmd = new OleDbCommand(sqlSelectTasks, connection))
                {

                    selectCmd.Parameters.AddWithValue("@todaysDate", todaysDate);

                    using (OleDbDataReader tasksReader = selectCmd.ExecuteReader())
                    {
                        while (tasksReader.Read())
                        {

                            long taskId = tasksReader.GetInt32(tasksReader.GetOrdinal("ID"));

                            _logger.LogInformation($"tasksReader.Read() {taskId} ");

                            string sqlLastTask = @"
                            SELECT * 
                            FROM TASKS 
                            WHERE [CHILD_OF_TASK_ID] = @taskId 
                            AND [REPEAT] = TRUE 
                            AND ([REPEAT_STOP_DATE] > @todaysDate OR [REPEAT_STOP_DATE] IS NULL) 
                            ORDER BY [DUE_DATE] DESC";

                            using (OleDbCommand selectCmd2 = new OleDbCommand(sqlLastTask, connection))
                            {
                                selectCmd2.Parameters.AddWithValue("@taskId", taskId);
                                selectCmd2.Parameters.AddWithValue("@todaysDate", todaysDate);

                                using (OleDbDataReader lastTaskReader = selectCmd2.ExecuteReader())
                                {
                                    if (!lastTaskReader.HasRows)
                                    {
                                        lastTaskReader.Close();

                                        // Use previous command's parameters
                                        selectCmd2.CommandText = "SELECT * FROM TASKS WHERE [ID] = @taskId";

                                        using (OleDbDataReader newLastTaskReader = selectCmd2.ExecuteReader())
                                        {
                                            ProcessLastTaskReader(newLastTaskReader, todaysDate, taskId, connectionstr);
                                        }
                                    }
                                    else
                                    {
                                        ProcessLastTaskReader(lastTaskReader, todaysDate, taskId, connectionstr);
                                    }

                                }
                            }
                        }
                    }
                }
                connection.Close();
            }
        }

        private void ProcessLastTaskReader(OleDbDataReader lastTaskReader, DateTime todaysDate, long taskId,string connectionstr)
        {
            _logger.LogInformation($"ProcessLastTaskReader");

            if (lastTaskReader.Read())
            {
                bool completePreviousFirst = lastTaskReader.GetBoolean(lastTaskReader.GetOrdinal("COMPLETE_PREVIOUS_FIRST"));
                bool completed = lastTaskReader.GetBoolean(lastTaskReader.GetOrdinal("COMPLETED"));
                int frequency = lastTaskReader.GetInt16(lastTaskReader.GetOrdinal("FREQUENCY"));

                if (completePreviousFirst && !completed)
                {
                    // Equivalent to "GoTo SKIP_THIS_TASK"
                    return;
                }

                if (frequency == 0)
                {
                    // Equivalent to "GoTo SKIP_THIS_TASK"
                    return;
                }

                DateTime lastDueDate = lastTaskReader.GetDateTime(lastTaskReader.GetOrdinal("DUE_DATE"));
                DateTime nextDueDate = lastDueDate;
                DateTime nextPeriodStartDate = DateTime.MinValue;
                DateTime repeatOnDate = DateTime.MinValue;
                DateTime firstReminderDate;
                int dayOfMonth;
                
                // Add your case handling logic here based on the VBA code you provided
                switch (frequency)
                {
                    case 1:
                        if (todaysDate.Year == 12)
                        {
                            nextPeriodStartDate = new DateTime(todaysDate.Year + 1, 1, 1);
                        }
                        else
                        {
                            nextPeriodStartDate = new DateTime(todaysDate.Year, todaysDate.Month + 1, 1);
                        }
                        nextPeriodStartDate = nextPeriodStartDate.AddMonths(1);
                        break;
                    case 2:
                        int daysToAdd = 7 - (int)todaysDate.DayOfWeek;
                        nextPeriodStartDate = todaysDate.AddDays(daysToAdd);
                        nextPeriodStartDate = nextPeriodStartDate.AddDays(7 * 6);
                        break;
                    case 3:
                        nextPeriodStartDate = new DateTime(todaysDate.Year + 1, 1, 1);
                        nextPeriodStartDate = nextPeriodStartDate.AddYears(1);
                        break;
                    case 4:
                        nextPeriodStartDate = todaysDate.AddDays(1);
                        nextPeriodStartDate = nextPeriodStartDate.AddDays(40);
                        break;
                    default:
                        break;
                }

                //int dayOfMonth;
                //DateTime repeatOnDate;
                //int dayOfWeek;
                //DateTime firstReminderDate;

                switch (frequency)
                {
                    case 1:
                        dayOfMonth = lastTaskReader.GetInt16(lastTaskReader.GetOrdinal("REPEAT_ON_DAY_OF_MONTH"));
                        nextDueDate = new DateTime(lastDueDate.AddMonths(1).Year, lastDueDate.AddMonths(1).Month, dayOfMonth);
                        break;
                    case 2:
                        nextDueDate = lastDueDate.AddDays(7);
                        break;
                    case 3:
                        repeatOnDate = lastTaskReader.GetDateTime(lastTaskReader.GetOrdinal("REPEAT_ON_DATE"));
                        nextDueDate = repeatOnDate;
                        repeatOnDate = nextDueDate.AddYears(1);
                        break;
                    case 4:
                        nextDueDate = lastDueDate.AddDays(1);
                        break;
                    default:
                        break;
                }

                var remindTaskDays = lastTaskReader.IsDBNull(lastTaskReader.GetOrdinal("REMIND_TASK_DAYS")) ? 0 : lastTaskReader.GetInt16(lastTaskReader.GetOrdinal("REMIND_TASK_DAYS"));
                firstReminderDate = nextDueDate.AddDays(-1 * remindTaskDays);
                long newTaskId=0;

                while (nextDueDate <= nextPeriodStartDate)
                {
                    _logger.LogInformation($"nextDueDate = {nextDueDate}");
                    using (OleDbConnection connection = new OleDbConnection(connectionstr))
                    {
                        connection.Open();
                        //Create new task for this nextDueDate
                        string sqlInsertTasks = @"
                        INSERT INTO TASKS ([DESC], DUE_DATE, REMARKS, REPEAT, FREQUENCY, REPEAT_ON_DAY_OF_MONTH, REPEAT_ON_DAY_OF_WEEK, REPEAT_ON_DATE, COMPLETE_PREVIOUS_FIRST, MARK_COMPLETED_ON_DISMISSED, CHILD_OF_TASK_ID, REMIND_TASK_DAYS, FIRST_REMINDER_DATE, [USER], DATEZ, REPEAT_STOP_DATE)
                        VALUES (@desc, @due_date, @remarks, @repeat, @frequency, @repeat_on_day_of_month, @repeat_on_day_of_week, @repeat_on_date, @complete_previous_first, @mark_completed_on_dismissed, @child_of_task_id, @remind_task_days, @first_reminder_date, @user, @datez, @repeat_stop_date)";
                        using (OleDbCommand insertCmd = new OleDbCommand(sqlInsertTasks, connection))
                        {
                            _logger.LogInformation($"X1");
                            // Add your parameters here
                            insertCmd.Parameters.AddWithValue("@desc", lastTaskReader["DESC"]);
                            insertCmd.Parameters.AddWithValue("@due_date", nextDueDate);
                            insertCmd.Parameters.AddWithValue("@remarks", lastTaskReader["REMARKS"]);
                            insertCmd.Parameters.AddWithValue("@repeat", lastTaskReader["REPEAT"]);
                            insertCmd.Parameters.AddWithValue("@frequency", lastTaskReader["FREQUENCY"]);
                            insertCmd.Parameters.AddWithValue("@repeat_on_day_of_month", lastTaskReader["REPEAT_ON_DAY_OF_MONTH"]);
                            insertCmd.Parameters.AddWithValue("@repeat_on_day_of_week", lastTaskReader["REPEAT_ON_DAY_OF_WEEK"]);
                            insertCmd.Parameters.AddWithValue("@repeat_on_date", lastTaskReader["REPEAT_ON_DATE"]);
                            insertCmd.Parameters.AddWithValue("@complete_previous_first", lastTaskReader["COMPLETE_PREVIOUS_FIRST"]);
                            insertCmd.Parameters.AddWithValue("@mark_completed_on_dismissed", lastTaskReader["MARK_COMPLETED_ON_DISMISSED"]);
                            insertCmd.Parameters.AddWithValue("@child_of_task_id", taskId);
                            insertCmd.Parameters.AddWithValue("@remind_task_days", lastTaskReader["REMIND_TASK_DAYS"]);
                            insertCmd.Parameters.AddWithValue("@first_reminder_date", firstReminderDate);
                            insertCmd.Parameters.AddWithValue("@user", lastTaskReader["USER"]);
                            insertCmd.Parameters.AddWithValue("@datez", lastTaskReader["DATEZ"]);
                            insertCmd.Parameters.AddWithValue("@repeat_stop_date", lastTaskReader["REPEAT_STOP_DATE"]);
                            _logger.LogInformation($"X2");
                            try
                            {
                                // Execute the insert statement and get the ID of the new task.
                                insertCmd.ExecuteScalar();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"X3:Error: {ex.Message}");
                            }

                            // Retrieve the ID of the last inserted record
                            string identityQuery = "SELECT @@IDENTITY";
                            
                            using (OleDbCommand identityCmd = new OleDbCommand(identityQuery, connection))
                            {
                                try
                                {
                                    newTaskId = (int)identityCmd.ExecuteScalar();
                                    _logger.LogInformation($"New Task ID: {newTaskId}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogInformation($"Error: {ex.Message}");
                                }
                            }

                        }

                        //Update this tasks users
                        string taskReminderUsersSql = $"SELECT * FROM TASK_REMINDER_USERS WHERE TASK = {taskId}";
                        _logger.LogInformation($"X4");
                        using (OleDbCommand taskReminderUsersCmd = new OleDbCommand(taskReminderUsersSql, connection))
                        {
                            using (OleDbDataReader reminderUserReader = taskReminderUsersCmd.ExecuteReader())
                            {
                                while (reminderUserReader.Read())
                                {
                                    // Create a new record in the TASK_REMINDER_USERS table with the same fields as the fetched record
                                    // except for the TASK field which should have the value stored in the taskId variable
                                    string insertSql = @"INSERT INTO TASK_REMINDER_USERS (TASK, [USER]) 
                                    VALUES (@task_id, @user)";

                                    using (OleDbCommand insertCmd = new OleDbCommand(insertSql, connection))
                                    {
                                        // Add your parameters here. The TASK field should have the value of the new taskId
                                        
                                        insertCmd.Parameters.AddWithValue("@task_id", newTaskId);  // newTaskId is your new taskId
                                        insertCmd.Parameters.AddWithValue("@user", reminderUserReader["USER"]);

                                        try
                                        {
                                            // Execute the insert statement
                                            insertCmd.ExecuteNonQuery();
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogInformation($"X5:Error: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }

                        string lastTaskSql = $@"
                        SELECT * 
                        FROM TASKS 
                        WHERE [CHILD_OF_TASK_ID] = {taskId} 
                        AND [REPEAT] = TRUE 
                        AND ([REPEAT_STOP_DATE] > @todaysDate OR ISDATE([REPEAT_STOP_DATE]) = 0) 
                        ORDER BY [DUE_DATE] DESC";
                        using (OleDbCommand lastTaskCmd = new OleDbCommand(lastTaskSql, connection))
                        {
                            lastTaskCmd.Parameters.AddWithValue("@todaysDate", todaysDate);

                            using (OleDbDataReader lastTaskReader2 = lastTaskCmd.ExecuteReader())
                            {
                                _logger.LogInformation($"X6");
                                if (lastTaskReader2.Read())
                                {
                                    lastDueDate = lastTaskReader2.GetDateTime(lastTaskReader2.GetOrdinal("DUE_DATE"));

                                    switch (frequency)
                                    {
                                        case 1:
                                            dayOfMonth = lastTaskReader.GetInt16(lastTaskReader2.GetOrdinal("REPEAT_ON_DAY_OF_MONTH"));
                                            nextDueDate = new DateTime(lastDueDate.AddMonths(1).Year, lastDueDate.AddMonths(1).Month, dayOfMonth);
                                            break;
                                        case 2:
                                            nextDueDate = lastDueDate.AddDays(7);
                                            break;
                                        case 3:
                                            repeatOnDate = lastTaskReader2.GetDateTime(lastTaskReader2.GetOrdinal("REPEAT_ON_DATE"));
                                            nextDueDate = repeatOnDate;
                                            repeatOnDate = nextDueDate.AddYears(1);
                                            break;
                                        case 4:
                                            nextDueDate = lastDueDate.AddDays(1);
                                            break;
                                    }
                                    var remindTaskDays2 = lastTaskReader2.IsDBNull(lastTaskReader2.GetOrdinal("REMIND_TASK_DAYS")) ? 0 : lastTaskReader2.GetInt16(lastTaskReader2.GetOrdinal("REMIND_TASK_DAYS"));
                                    firstReminderDate = nextDueDate.AddDays(-1 * remindTaskDays2);
                                }
                            }
                        }
                        connection.Close();
                    }
                }

            }
        }

    }
}
