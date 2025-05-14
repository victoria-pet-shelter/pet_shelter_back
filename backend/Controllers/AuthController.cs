using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Validation;
using Config;
using Models;
using Dtos;

namespace Controllers;

[ApiController]
[Route("/")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext db;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext db, IConfiguration configuration)
    {
        this.db = db;
        _jwtService = new JwtService(configuration);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto user)
    {
        var validator = new UserRegisterValidator();
        var errors = validator.Validate(user);
        if (errors.Any())
            return BadRequest(new { errors });

        try
        {
            if (await db.Users.AnyAsync(u => u.name == user.name))
                return BadRequest("Username already exists.");

            if (await db.Users.AnyAsync(u => u.email == user.email))
                return BadRequest("Email already exists.");

            var newUser = new Users
            {
                id = Guid.NewGuid(),
                name = user.name,
                email = user.email,
                password = BCrypt.Net.BCrypt.HashPassword(user.password),
                role = user.role
            };

            await db.Users.AddAsync(newUser);
            await db.SaveChangesAsync();

            return Ok("User registered successfully.");
        }
        catch (Exception ex)
        {
            return Problem("Error: " + ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginRequest)
    {
        var validator = new UserLoginValidator();
        var errors = validator.Validate(loginRequest);
        if (errors.Any()) 
            return BadRequest(new { errors });

        try
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.email == loginRequest.email);
            if (user == null)
                return Unauthorized("Incorrect email or password.");

            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(loginRequest.password, user.password);
            if (!isPasswordCorrect)
                return Unauthorized("Incorrect email or password.");

            string token = _jwtService.GenerateToken(user.id, user.role ?? "user");

            return Ok(new
            {
                token,
                id = user.id,
                name = user.name,
                email = user.email,
                role = user.role
            });
        }
        catch (Exception ex)
        {
            return Problem("Error: " + ex.Message);
        }
    }
}