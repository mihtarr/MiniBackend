using MiniBackend.Data;
using MiniBackend.Models;

namespace MiniBackend.Services
{
    public class AuthHelper
    {
        private readonly AppDbContext _db;

        public AuthHelper(AppDbContext db)
        {
            _db = db;
        }

        public User? GetUserFromToken(string token)
        {
            var session = _db.Sessions.FirstOrDefault(s => s.Token == token && s.Expiry > DateTime.UtcNow);
            if (session == null) return null;

            return _db.Users.FirstOrDefault(u => u.Id == session.UserId);
        }
    }
}
