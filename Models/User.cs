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

        // Email onay için ek alanlar
        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Yeni email onayı için
        public string? PendingNewEmail { get; set; }
        public string? NewEmailConfirmationToken { get; set; }
    }
}
