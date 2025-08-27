using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
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
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var key = Encoding.ASCII.GetBytes(_config["JwtKey"] ?? "f8G7#d2!KqL9vPzX1mN6@bR4yT0wZ3eH");
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);

                return _db.Users.FirstOrDefault(u => u.Id == userId);
            }
            catch
            {
                return null; // Geçersiz veya süresi dolmuş token
            }
        }
    }
}
