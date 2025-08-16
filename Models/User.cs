namespace MiniBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // Basit demo, hashleme ileride eklenebilir
        public bool IsActive { get; set; } // Abonelik durumu
    }
}
