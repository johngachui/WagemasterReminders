namespace WagemasterEvents.Models
{
    public class Settings
    {
        public int Id { get; set; }
        public string ?Server { get; set; }
        public int CacheTime { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    
}
