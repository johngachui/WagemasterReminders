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
        private OleDbConnection connection;

        public void CreateRepeatingTasks(string databasePath, OleDbConnection connection)
        {
            DateTime todaysDate = DateTime.Today;
            //DateTime lastDueDate, nextDueDate, nextPeriodStartDate, firstReminderDate, repeatOnDate;
            //int frequency, dayOfMonth;
            //long idX;

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
                        long taskId = tasksReader.GetInt64(tasksReader.GetOrdinal("ID"));
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
                                        ProcessLastTaskReader(newLastTaskReader, todaysDate, taskId);
                                    }
                                }
                                else
                                {
                                    ProcessLastTaskReader(lastTaskReader, todaysDate, taskId);
                                }
                                
                            }
                        }
                    }
                }
            }
            
        }

        private void ProcessLastTaskReader(OleDbDataReader lastTaskReader, DateTime todaysDate, long taskId)
        {
            if (lastTaskReader.Read())
            {
                bool completePreviousFirst = lastTaskReader.GetBoolean(lastTaskReader.GetOrdinal("COMPLETE_PREVIOUS_FIRST"));
                bool completed = lastTaskReader.GetBoolean(lastTaskReader.GetOrdinal("COMPLETED"));
                int frequency = lastTaskReader.GetInt32(lastTaskReader.GetOrdinal("FREQUENCY"));

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
                int dayOfWeek;
                //DateTime firstReminderDate;

                switch (frequency)
                {
                    case 1:
                        dayOfMonth = lastTaskReader.GetInt32(lastTaskReader.GetOrdinal("REPEAT_ON_DAY_OF_MONTH"));
                        nextDueDate = new DateTime(lastDueDate.AddMonths(1).Year, lastDueDate.AddMonths(1).Month, dayOfMonth);
                        break;
                    case 2:
                        dayOfWeek = lastTaskReader.GetInt32(lastTaskReader.GetOrdinal("REPEAT_ON_DAY_OF_WEEK"));
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
                        throw new Exception("Invalid frequency value");
                }

                var remindTaskDays = lastTaskReader.IsDBNull(lastTaskReader.GetOrdinal("REMIND_TASK_DAYS")) ? 0 : lastTaskReader.GetInt32(lastTaskReader.GetOrdinal("REMIND_TASK_DAYS"));
                firstReminderDate = nextDueDate.AddDays(-1 * remindTaskDays);

                while (nextDueDate <= nextPeriodStartDate)
                {
                    string taskReminderUsersSql = $"SELECT * FROM TASK_REMINDER_USERS WHERE TASK = {taskId}";
                    // replace YourOleDbConnection with the actual name of your OleDbConnection object
                    using (OleDbCommand taskReminderUsersCmd = new OleDbCommand(taskReminderUsersSql, connection))
                    {
                        // Here you can process the TASK_REMINDER_USERS records associated with the task
                        // ...
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
                            if (lastTaskReader2.Read())
                            {
                                lastDueDate = lastTaskReader2.GetDateTime(lastTaskReader2.GetOrdinal("DUE_DATE"));

                                switch (frequency)
                                {
                                    case 1:
                                        dayOfMonth = lastTaskReader.GetInt32(lastTaskReader2.GetOrdinal("REPEAT_ON_DAY_OF_MONTH"));
                                        nextDueDate = new DateTime(lastDueDate.AddMonths(1).Year, lastDueDate.AddMonths(1).Month, dayOfMonth);
                                        break;
                                    case 2:
                                        dayOfWeek = lastTaskReader2.GetInt32(lastTaskReader2.GetOrdinal("REPEAT_ON_DAY_OF_WEEK"));
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
                                var remindTaskDays2 = lastTaskReader2.IsDBNull(lastTaskReader2.GetOrdinal("REMIND_TASK_DAYS")) ? 0 : lastTaskReader2.GetInt32(lastTaskReader2.GetOrdinal("REMIND_TASK_DAYS"));
                                firstReminderDate = nextDueDate.AddDays(-1 * remindTaskDays2);
                            }
                        }
                    }
                }

            }
        }

    }
}
