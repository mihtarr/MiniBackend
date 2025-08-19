namespace MiniBackend.Models
{
    public class ChangeEmailRequest
    {
        public int UserId { get; set; }
        public string NewEmail { get; set; } = null!;
    }
}
