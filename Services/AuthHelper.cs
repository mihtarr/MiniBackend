using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using MiniBackend.Data;
using MiniBackend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;


namespace MiniBackend.Services
{
    public class AuthHelper
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthHelper(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_config["JwtKey"] ?? "f8G7#d2!KqL9vPzX1mN6@bR4yT0wZ3eH");
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim("nameid", user.Id.ToString()),   // ✅ burada "nameid"
            new Claim("unique_name", user.Username)    // aynı şekilde "unique_name"
        }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }


        public async Task<User?> GetUserFromToken(string token)
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

                // "nameid" veya ClaimTypes.NameIdentifier ikisini de destekle
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == ClaimTypes.NameIdentifier || x.Type == "nameid");

                if (userIdClaim == null) return null;

                var userId = int.Parse(userIdClaim.Value);

                return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch
            {
                return null; // Geçersiz veya süresi dolmuş token
            }
        }

    }
}
