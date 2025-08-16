using Microsoft.AspNetCore.Mvc;
using MiniBackend.Data;
using MiniBackend.Models;

namespace MiniBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] User user)
    {
        if (_context.Users.Any(u => u.Username == user.Username))
            return BadRequest("Username already exists");

        _context.Users.Add(user);
        _context.SaveChanges();
        return Ok("User registered");
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] User login)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == login.Username && u.Password == login.Password);
        if (user == null) return Unauthorized("Invalid credentials");

        var token = Guid.NewGuid().ToString();
        var session = new Session
        {
            UserId = user.Id,
            Token = token,
            Expiry = DateTime.UtcNow.AddHours(1)
        };
        _context.Sessions.Add(session);
        _context.SaveChanges();

        return Ok(new { Token = token });
    }
}
