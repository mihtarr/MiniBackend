namespace MiniBackend.Models
{
    public class Session
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime StartedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
