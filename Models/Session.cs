namespace MiniBackend.Models
{
    public class Session
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime Expiry { get; set; }
    }
}
