namespace YourProjectName.Models
{
    public class Event
    {
        public int ID { get; set; }
        public string? RefNum { get; set; }
        public string? RefName { get; set; }
        public string? ReminderType { get; set; }
        public string? ReminderMsg { get; set; }
        public DateTime? RefDate { get; set; }
        public DateTime? ReminderDate { get; set; }
        public string? DatabasePath { get; set; }
        public string? Company { get; set; }
        public bool Dismissed { get; set; }
        public int? Ref_ID { get; set; }
        
    }
}
