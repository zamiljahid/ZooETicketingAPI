using Microsoft.AspNetCore.Mvc;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;
namespace ZooTicketing.API.Controllers;
[ApiController][Route("api/auth")]
public class AuthController(IAuthRepository repo) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req) {
        try {
            if(string.IsNullOrWhiteSpace(req.Email)||string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Email and password are required.");
            var r = await repo.LoginAsync(req);
            return r is null ? Unauthorized("Invalid email or password.") : Ok(r);
        }
        catch(InvalidOperationException ex){ return BadRequest(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Login failed: {ex.Message}"); }
    }

    [HttpGet("find")]
    public async Task<IActionResult> Find([FromQuery]string query) {
        try {
            if(string.IsNullOrWhiteSpace(query)) return BadRequest("A phone number or email is required.");
            var r = await repo.FindByContactAsync(query);
            return r is null ? NotFound("No account found for that phone or email.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Lookup failed: {ex.Message}"); }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req) {
        try {
            var ok = await repo.RegisterAsync(req);
            return ok ? Ok("Registered successfully.") : Conflict("Email already exists.");
        }
        catch(InvalidOperationException ex){ return BadRequest(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Registration failed: {ex.Message}"); }
    }
}
