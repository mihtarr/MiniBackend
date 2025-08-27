using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using MiniBackend.Data;
using MiniBackend.Models;
using MiniBackend.Services;
using MiniBackend.Helpers;

namespace MiniBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, EmailService emailService, IConfiguration config)
        {
            _db = db;
            _emailService = emailService;
            _config = config; // <-- Burada atanıyor
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username, email and password are required");

            if (_db.Users.Any(u => u.Username == request.Username))
                return BadRequest("Username already exists");
            if (_db.Users.Any(u => u.Email == request.Email))
                return BadRequest("Email already exists");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = PasswordHelper.HashPassword(request.Password),
                EmailConfirmationToken = Guid.NewGuid().ToString()
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var confirmLink = $"https://minibackend-zwep.onrender.com/api/auth/confirm-email?token={user.EmailConfirmationToken}";
            await _emailService.SendConfirmationEmail(user.Email, confirmLink);

            return Ok("User registered successfully. Please confirm your email.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password cannot be empty");

            // Kullanıcıyı username ile alıyoruz
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return Unauthorized("Invalid credentials");

            // Şifre doğrulama (hash ile)
            if (!PasswordHelper.VerifyPassword(request.Password, user.Password))
                return Unauthorized("Invalid credentials");

            if (!user.IsEmailConfirmed)
                return Unauthorized("Please confirm your email");

            // JWT token oluştur
            var token = GenerateJwtToken(user);

            return Ok(new { Token = token });
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return BadRequest("Email not registered");

            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiration = DateTime.UtcNow.AddHours(1);
            await _db.SaveChangesAsync();

            var resetLink = $"https://minifrontend-6ivp.onrender.com/password.html?token={user.ResetToken}";
            await _emailService.SendResetPasswordEmail(user.Email, resetLink);

            return Ok("Reset link sent to email");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.ResetToken == request.Token && u.ResetTokenExpiration > DateTime.UtcNow);

            if (user == null) return BadRequest("Invalid or expired token");

            user.Password = PasswordHelper.HashPassword(request.NewPassword); // TODO: hashle
            user.ResetToken = null;
            user.ResetTokenExpiration = null;

            await _db.SaveChangesAsync();
            return Ok("Password reset successfully");
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token)) return Unauthorized("Missing token.");

            var authHelper = new AuthHelper(_db);
            var user = authHelper.GetUserFromToken(token);

            if (user == null) return Unauthorized("Invalid or expired token.");

            // Hash'lenmiş şifre kontrolü
            if (!PasswordHelper.VerifyPassword(request.OldPassword, user.Password))
                return BadRequest("Old password is incorrect.");

            if (request.NewPassword.Length < 8)
                return BadRequest("New password must be at least 8 characters long.");

            user.Password = PasswordHelper.HashPassword(request.NewPassword);
            await _db.SaveChangesAsync();

            return Ok("Password changed successfully.");
        }


        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);
            if (user == null) return BadRequest("Invalid token");

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            await _db.SaveChangesAsync();

            return Ok("Email confirmed");
        }

        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token)) return Unauthorized("Missing token.");

            var authHelper = new AuthHelper(_db);
            var user = authHelper.GetUserFromToken(token);

            if (user == null) return Unauthorized("Invalid or expired token.");

            if (_db.Users.Any(u => u.Email == request.NewEmail))
                return BadRequest("Email already used");

            user.NewEmail = request.NewEmail;
            user.NewEmailConfirmationToken = Guid.NewGuid().ToString();
            await _db.SaveChangesAsync();

            var confirmLink = $"https://minibackend-zwep.onrender.com/api/auth/confirm-new-email?token={user.NewEmailConfirmationToken}";
            await _emailService.SendConfirmationEmail(user.NewEmail, confirmLink);

            return Ok("Confirmation link sent to new email");
        }


        [HttpGet("confirm-new-email")]
        public async Task<IActionResult> ConfirmNewEmail([FromQuery] string token)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.NewEmailConfirmationToken == token);
            if (user == null) return BadRequest("Invalid token");

            user.Email = user.NewEmail!;
            user.NewEmail = null;
            user.NewEmailConfirmationToken = null;
            await _db.SaveChangesAsync();

            return Ok("New email confirmed");
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            string email = request.Email?.Trim() ?? "";

            if (string.IsNullOrEmpty(email)) return BadRequest("Email cannot be empty");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return BadRequest("User not found.");
            if (user.IsEmailConfirmed) return BadRequest("This email is already confirmed.");

            // Generate new token
            user.EmailConfirmationToken = Guid.NewGuid().ToString();

            // Save changes
            await _db.SaveChangesAsync();

            // Send new confirmation email
            var confirmLink = $"https://minibackend-zwep.onrender.com/api/auth/confirm-email?token={user.EmailConfirmationToken}";

            await _emailService.SendConfirmationEmail(user.Email, confirmLink);
            return Ok("A new confirmation email has been sent.");
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_config["JwtKey"] ?? "f8G7#d2!KqL9vPzX1mN6@bR4yT0wZ3eH");
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }


    }
}
