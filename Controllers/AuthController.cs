using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniBackend.Data;
using MiniBackend.Models;

namespace MiniBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User login)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == login.Username && u.Password == login.Password);
            if (user == null || !user.IsActive) return Unauthorized(new { message = "Invalid credentials or inactive subscription" });

            // Tek aktif oturum kontrolü
            var activeSession = await _context.Sessions.FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive);
            if (activeSession != null) return Conflict(new { message = "User already has an active session" });

            var session = new Session { UserId = user.Id, StartedAt = DateTime.UtcNow, IsActive = true };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(new { sessionId = session.Id });
        }

        [HttpPost("session/check")]
        public async Task<IActionResult> CheckSession([FromBody] int sessionId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null || !session.IsActive) return Unauthorized(new { message = "Invalid or expired session" });

            return Ok(new { message = "Session valid" });
        }

        [HttpPost("session/end")]
        public async Task<IActionResult> EndSession([FromBody] int sessionId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return NotFound();

            session.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Session ended" });
        }
    }
}
