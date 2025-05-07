using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Config;
using Microsoft.Extensions.Configuration;

namespace Controllers;

[ApiController]
[Route("/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _jwtService = new JwtService(configuration);
    }

    // Register endpoint
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Users user)
    {
        // Have a user with these email
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.email == user.email);
        if (existingUser != null)
        {
            return BadRequest("A user with this email already exists.");
        }

        user.id = Guid.NewGuid();
        user.password = BCrypt.Net.BCrypt.HashPassword(user.password); // Hash password

        // Post user to database
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return Ok("User registered successfully.");
    }

    // Login endpoint
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Users loginRequest)
    {
        // Have a user with these email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.email == loginRequest.email);
        if (user == null)
        {
            return Unauthorized("Incorrect email or password.");
        }

        // Check password
        bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(loginRequest.password, user.password);
        if (!isPasswordCorrect)
        {
            return Unauthorized("Incorrect email or password..");
        }

        string token = _jwtService.GenerateToken(user.id, user.role ?? "user");

        // Return token to client
        return Ok(new { token = token });
    }
}
