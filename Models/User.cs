namespace MiniBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Email { get; set; } = null!;

        // Şifre reset token alanları
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiration { get; set; }
    }
}
