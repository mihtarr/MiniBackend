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
        public IActionResult Register([FromBody] User user)
        {
            if (_db.Users.Any(u => u.Username == user.Username))
                return BadRequest("Username already exists");

            _db.Users.Add(user);
            _db.SaveChanges();
            return Ok("User registered");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User login)
        {
            var user = _db.Users.FirstOrDefault(u => u.Username == login.Username && u.Password == login.Password);
            if (user == null) return Unauthorized("Invalid credentials");

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
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Ok(); // Mail yoksa bile başarılı gibi göster

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
                _emailService.SendResetPasswordEmail(user.Email, resetLink);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error processing password reset: " + ex.Message);
            }
        }
    }
}
