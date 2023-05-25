using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace WagemasterEvents.Models
{
    public class Event : INotifyPropertyChanged
    {
        private int id;
        private string ?refno;
        private string ?refname;
        private string ?reminderType;
        private string ?reminder;
        private DateTime dueDate;
        private DateTime nextReminderDate;
        private string ?databasePath;
        private string ?company;
        private bool dismissed;

        [JsonPropertyName("id")]
        public int ID
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("refNum")]
        public string Refno
        {
            get { return refno; }
            set
            {
                refno = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("refName")]
        public string Refname
        {
            get { return refname; }
            set
            {
                refname = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("reminderType")]
        public string ReminderType
        {
            get { return reminderType; }
            set
            {
                reminderType = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("reminderMsg")]
        public string Reminder
        {
            get { return reminder; }
            set
            {
                reminder = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("refDate")]
        public string DueDate
        {
            get { return dueDate.ToString("D"); }
            set
            {
                dueDate = DateTime.Parse(value);
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("reminderDate")]
        public DateTime NextReminderDate
        {
            get { return nextReminderDate; }
            set
            {
                nextReminderDate = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("databasePath")]
        public string DatabasePath
        {
            get { return databasePath; }
            set
            {
                databasePath = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("company")]
        public string Company
        {
            get { return company; }
            set
            {
                company = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("dismissed")]
        public bool Dismissed
        {
            get { return dismissed; }
            set
            {
                dismissed = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
