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
public IActionResult Register([FromBody] RegisterRequest request)
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
        Password = password
    };

    _db.Users.Add(user);
    _db.SaveChanges();

    return Ok("User registered successfully");
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

    //if (string.IsNullOrEmpty(email))
        //return BadRequest("Email cannot be empty");

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
        _emailService.SendResetPasswordEmail(request.Email, resetLink);
        return Ok("A reset link has been sent to your email.");
    }
    catch (Exception ex)
    {
        return StatusCode(500, "Error processing password reset: " + ex.Message);
    }
}

    }
}
