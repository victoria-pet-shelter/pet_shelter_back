using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Config;
using Microsoft.Extensions.Configuration;
using Dtos;

namespace Controllers;

[ApiController]
[Route("/")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _jwtService = new JwtService(configuration);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto user)
    {
        if (user.email == null || user.email.Trim() == "")
            return BadRequest("Email is required.");
        if (user.password == null || user.password.Trim() == "")
            return BadRequest("Password is required.");
        if (user.name == null || user.name.Trim() == "")
            return BadRequest("Username is required.");

        if (user.email.Length < 5 || user.email.Length > 50)
            return BadRequest("Email must be between 5 and 50 characters.");
        if (user.name.Length < 3 || user.name.Length > 20)
            return BadRequest("Username must be between 3 and 20 characters.");
        if (user.password.Length < 6 || user.password.Length > 20)
            return BadRequest("Password must be between 6 and 20 characters.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(user.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return BadRequest("Email is not valid.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(user.password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{6,}$"))
            return BadRequest("Password must contain at least one uppercase letter, one lowercase letter, and one number.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(user.name, @"^[a-zA-Z0-9]+$"))
            return BadRequest("Username must contain only letters and numbers.");

        var existingUserName = await _context.Users.FirstOrDefaultAsync(u => u.name == user.name);
        if (existingUserName != null)
            return BadRequest("Username already exists.");

        var existingUserEmail = await _context.Users.FirstOrDefaultAsync(u => u.email == user.email);
        if (existingUserEmail != null)
            return BadRequest("Email already exists.");

        var newUser = new Users
        {
            id = Guid.NewGuid(),
            name = user.name,
            email = user.email,
            password = BCrypt.Net.BCrypt.HashPassword(user.password),
            role = "user"
        };

        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();

        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginRequest)
    {
        if (loginRequest.email == null || loginRequest.email.Trim() == "")
            return BadRequest("Email must be provided.");
        if (loginRequest.password == null || loginRequest.password.Trim() == "")
            return BadRequest("Password must be provided.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.email == loginRequest.email);
        if (user == null)
            return Unauthorized("Incorrect email or password.");

        bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(loginRequest.password, user.password);
        if (!isPasswordCorrect)
            return Unauthorized("Incorrect email or password.");

        string token = _jwtService.GenerateToken(user.id, user.role ?? "user");

        return Ok(new
        {
            token = token,
            id = user.id,
            name = user.name,
            email = user.email,
            role = user.role
        });
    }
}
