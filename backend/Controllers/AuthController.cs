using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Config;
using Microsoft.Extensions.Configuration;

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

    // Register endpoint
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Users user)
    {
        // Validation block

        // Check if the user is null or empty
        if (string.IsNullOrEmpty(user.email))
        {
            return BadRequest("Email is null or Empty.");
        }
        else if (string.IsNullOrEmpty(user.password))
        {
            return BadRequest("Password is null or Empty.");
        }
        else if (string.IsNullOrEmpty(user.name))
        {
            return BadRequest("Username is null or Empty.");
        }

        // Check length of user dates
        if (user.email.Length < 5 || user.email.Length > 50)
        {
            return BadRequest("email must be between 5 and 50 characters.");
        }
        else if (user.name.Length < 3 || user.name.Length > 20)
        {
            return BadRequest("User name must be between 3 and 20 characters.");
        }
        else if (user.password.Length < 6 || user.password.Length > 20)
        {
            return BadRequest("Password must be between 6 and 20 characters.");
        }

        // Check if the email is valid
        if (!System.Text.RegularExpressions.Regex.IsMatch(user.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return BadRequest("Email is not valid.");
        }
        // Check if the password is valid
        if (!System.Text.RegularExpressions.Regex.IsMatch(user.password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{6,}$"))
        {
            return BadRequest("Password must contain at least one uppercase letter, one lowercase letter, and one number.");
        }
        // Check if the username is valid
        if (!System.Text.RegularExpressions.Regex.IsMatch(user.name, @"^[a-zA-Z0-9]+$"))
        {
            return BadRequest("Username must contain only letters and numbers.");
        }

        // Check if the username is already taken
        var existingUserName = await _context.Users.FirstOrDefaultAsync(u => u.name == user.name);
        if (existingUserName != null)
        {
            return BadRequest("A user with this username already exists.");
        }
        // Check if the email is already taken
        var existingUserEmail = await _context.Users.FirstOrDefaultAsync(u => u.email == user.email);
        if (existingUserEmail != null)
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
        // Validation block
        // Check if the user is null or empty
        if (loginRequest.email == null || loginRequest.email.Trim() == "")
        {
            return BadRequest("Email must be provided.");
        }
        else if (loginRequest.password == null || loginRequest.password.Trim() == "")
        {
            return BadRequest("Password must be provided.");
        }

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
