namespace YourProjectName.Models
{
    public class UpdateEventRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? DatabasePath { get; set; }
        public bool Dismissed { get; set; }
        public int Ref_ID { get; set; }
        public string? ReminderType { get; set; }

    }
}
