using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using Dapper;
using WagemasterEvents.Models;

namespace WagemasterEvents.Database
{
    public static class EventsRepository
    {
        public static List<Event> GetEvents(bool showDismissed)
        {
            using (IDbConnection connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                var events = connection.Query<Event>("SELECT * FROM EventsList WHERE Dismissed = 0 OR Dismissed = @ShowDismissed", new { ShowDismissed = showDismissed ? 1 : 0 }).AsList();
                return events;
            }
        }

        public static void SaveEvents(IEnumerable<Event> events)
        {
            using (IDbConnection connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                foreach (var eventItem in events)
                {
                    var existingEvent = connection.Query<Event>("SELECT * FROM EventsList WHERE Company = @Company AND ReminderType = @ReminderType AND Reminder = @Reminder AND DueDate = @DueDate AND NextReminderDate = @NextReminderDate AND Dismissed = @Dismissed AND Refno = @Refno AND DatabasePath = @DatabasePath AND Refname = @Refname", eventItem).FirstOrDefault();

                    if (existingEvent == null)
                    {
                        connection.Execute("INSERT INTO EventsList (Company, ReminderType, Reminder, DueDate, NextReminderDate, Dismissed, Refno, DatabasePath, Refname) VALUES (@Company, @ReminderType, @Reminder, @DueDate, @NextReminderDate, @Dismissed, @Refno, @DatabasePath, @Refname)", eventItem);
                    }
                }
            }
        }


        public static void UpdateEvent(Event eventItem)
        {
            using (IDbConnection connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Execute("UPDATE EventsList SET NextReminderDate = @NextReminderDate, Dismissed = @Dismissed WHERE id = @id", eventItem);
            }
        }
    }
}
