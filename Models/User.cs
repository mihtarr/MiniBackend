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

        // Email onay alanları
        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }

        // Yeni email değişikliği için
        public string? NewEmail { get; set; }
        public string? NewEmailConfirmationToken { get; set; }
    }
}
