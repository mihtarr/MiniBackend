using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniBackend.Data;
using MiniBackend.Models;
using MiniBackend.Services;

namespace MiniBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmailService _emailService;

        public AuthController(AppDbContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            string username = request.Username?.Trim() ?? "";
            string email = request.Email?.Trim() ?? "";
            string password = request.Password ?? "";

            if (string.IsNullOrEmpty(username))
                return BadRequest("Username cannot be empty");

            if (string.IsNullOrEmpty(email))
                return BadRequest("Email cannot be empty");

            if (string.IsNullOrEmpty(password))
                return BadRequest("Password cannot be empty");

            if (password.Length < 8)
                return BadRequest("Password must be at least 8 characters long");

            if (_db.Users.Any(u => u.Username == username))
                return BadRequest("Username already exists");

            if (_db.Users.Any(u => u.Email == email))
                return BadRequest("Email already exists");

            var user = new User
            {
                Username = username,
                Email = email,
                Password = password,
                IsEmailConfirmed = false,
                EmailConfirmationToken = Guid.NewGuid().ToString()
            };

            // Email onay linki
            var confirmLink = $"https://minibackend-zwep.onrender.com/api/auth/confirm-email?token={user.EmailConfirmationToken}";
            await _emailService.SendConfirmationEmail(user.Email, confirmLink);
            // TODO: istersen ayrı SendConfirmationEmail metodu ekleyebilirsin

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok("User registered successfully. Please check your email to confirm your account.");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            string username = request.Username?.Trim() ?? "";
            string password = request.Password ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return BadRequest("Username and password cannot be empty");

            var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!user.IsEmailConfirmed)
                return Unauthorized("Please confirm your email before logging in.");

            var token = Guid.NewGuid().ToString();
            var session = new Session
            {
                UserId = user.Id,
                Token = token,
                Expiry = DateTime.UtcNow.AddHours(1)
            };

            _db.Sessions.Add(session);
            _db.SaveChanges();

            return Ok(new { Token = token });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            string email = request.Email?.Trim() ?? "";

            if (string.IsNullOrEmpty(email))
                return BadRequest("Email cannot be empty");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return BadRequest("Email not registered");

            // Generate reset token
            var resetToken = Guid.NewGuid().ToString();

            // Save token to DB
            user.ResetToken = resetToken;
            user.ResetTokenExpiration = DateTime.UtcNow.AddHours(1);
            await _db.SaveChangesAsync();

            // Send email
            try
            {
                var resetLink = $"https://minifrontend-6ivp.onrender.com/reset-password.html?token={resetToken}";
                await _emailService.SendResetPasswordEmail(user.Email, resetLink);
                return Ok("A reset link has been sent to your email.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error processing password reset: " + ex.Message);
            }
        }

        [HttpGet("confirm-email")] // Yeni kullanıcı oluştururken // sakın silme!! mail onay linke tıklayınca buradan kontrol ediliyor.
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            var user = _db.Users.FirstOrDefault(u => u.EmailConfirmationToken == token);

            if (user == null)
                return BadRequest("Invalid token.");

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            await _db.SaveChangesAsync();

            return Ok("Your email has been confirmed! You can now login.");
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
                return BadRequest("This email is already registered.");

            user.NewEmail = request.NewEmail;
            user.NewEmailConfirmationToken = Guid.NewGuid().ToString();

            var confirmLink = $"https://minibackend-zwep.onrender.com/api/auth/confirm-new-email?token={user.NewEmailConfirmationToken}";
            await _emailService.SendConfirmationEmail(user.NewEmail, confirmLink);

            await _db.SaveChangesAsync();

            return Ok("Confirmation link has been sent to your new email. Please confirm to activate it.");
        }


        [HttpGet("confirm-new-email")]
        public async Task<IActionResult> ConfirmNewEmail([FromQuery] string token)
        {
            var user = _db.Users.FirstOrDefault(u => u.NewEmailConfirmationToken == token);
            if (user == null) return BadRequest("Invalid token.");

            user.Email = user.NewEmail!;
            user.NewEmail = null;
            user.NewEmailConfirmationToken = null;
            await _db.SaveChangesAsync();

            return Ok("Your new email has been confirmed.");
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token)) return Unauthorized("Missing token.");

            var authHelper = new AuthHelper(_db);
            var user = authHelper.GetUserFromToken(token);
            if (user == null) return Unauthorized("Invalid or expired token.");

            if (user.Password != request.OldPassword)
                return BadRequest("Old password is incorrect.");

            if (request.NewPassword.Length < 8)
                return BadRequest("New password must be at least 8 characters long.");

            user.Password = request.NewPassword;
            await _db.SaveChangesAsync();

            return Ok("Password changed successfully.");
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            string email = request.Email?.Trim() ?? "";

            if (string.IsNullOrEmpty(email))
                return BadRequest("Email cannot be empty");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return BadRequest("User not found.");

            if (user.IsEmailConfirmed)
                return BadRequest("This email is already confirmed.");

            // Generate new token
            user.EmailConfirmationToken = Guid.NewGuid().ToString();

            // Save changes
            await _db.SaveChangesAsync();

            // Send new confirmation email
            var confirmLink = $"https://minibackend-zwep.onrender.com/api/auth/confirm-email?token={user.EmailConfirmationToken}";
            await _emailService.SendConfirmationEmail(user.Email, confirmLink);

            return Ok("A new confirmation email has been sent.");
        }
    }
}
