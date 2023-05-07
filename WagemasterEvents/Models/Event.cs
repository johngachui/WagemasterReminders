using System;
using System.Text.Json.Serialization;

namespace WagemasterEvents.Models
{
    public class Event
    {
        [JsonPropertyName("refnum")]
        public string Refno { get; set; }

        [JsonPropertyName("refname")]
        public string Refname { get; set; }

        [JsonPropertyName("reminderType")]
        public string ReminderType { get; set; }

        [JsonPropertyName("reminderMsg")]
        public string Reminder { get; set; }

        [JsonPropertyName("refdate")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("reminderDate")]
        public DateTime NextReminderDate { get; set; }

        [JsonPropertyName("databasePath")]
        public string DatabasePath { get; set; }

        [JsonPropertyName("company")]
        public string Company { get; set; }

        public bool Dismissed { get; set; }
    }
}

