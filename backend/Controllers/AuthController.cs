using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Models;
using Config;
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
        if (string.IsNullOrWhiteSpace(user.email))
            return BadRequest("Email is required.");
        if (string.IsNullOrWhiteSpace(user.password))
            return BadRequest("Password is required.");
        if (string.IsNullOrWhiteSpace(user.name))
            return BadRequest("Name is required.");
        if (user.role != "user" && user.role != "shelter")
            return BadRequest("Role must be either 'user' or 'shelter'.");

        if (user.email.Length < 5 || user.email.Length > 50)
            return BadRequest("Email must be between 5 and 50 characters.");
        if (user.name.Length < 3 || user.name.Length > 20)
            return BadRequest("Name must be between 3 and 20 characters.");
        if (user.password.Length < 6 || user.password.Length > 20)
            return BadRequest("Password must be between 6 and 20 characters.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(user.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return BadRequest("Email is not valid.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(user.password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]{6,}$"))
            return BadRequest("Password must contain at least one uppercase letter, one lowercase letter, one number, and allowed special characters.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(user.name, @"^[a-zA-Z0-9]+$"))
            return BadRequest("Name must contain only letters and numbers.");

        var emailExistsInUsers = await db.Users.AnyAsync(u => u.email == user.email);
        var emailExistsInShelters = await db.Shelters.AnyAsync(s => s.email == user.email);
        if (emailExistsInUsers || emailExistsInShelters)
            return BadRequest("Email already exists.");

        if (user.role == "user")
        {
            var newUser = new Users
            {
                id = Guid.NewGuid(),
                name = user.name,
                email = user.email,
                password = BCrypt.Net.BCrypt.HashPassword(user.password),
                role = "user"
            };

            await db.Users.AddAsync(newUser);
        }
        else if (user.role == "shelter")
        {
            if (string.IsNullOrWhiteSpace(user.address))
                return BadRequest("Address is required for shelters.");
            if (string.IsNullOrWhiteSpace(user.phone))
                return BadRequest("Phone is required for shelters.");

            var newShelter = new Shelters
            {
                id = Guid.NewGuid(),
                shelter_owner_id = Guid.NewGuid(),
                name = user.name,
                email = user.email,
                phone = user.phone,
                address = user.address,
                description = user.description ?? "",
                created_at = DateTime.UtcNow
            };

            await db.Shelters.AddAsync(newShelter);
        }

        await db.SaveChangesAsync();
        return Ok("Registration successful.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginRequest)
    {
        if (string.IsNullOrWhiteSpace(loginRequest.email))
            return BadRequest("Email must be provided.");
        if (string.IsNullOrWhiteSpace(loginRequest.password))
            return BadRequest("Password must be provided.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.email == loginRequest.email);

        if (user == null)
        {
            var shelter = await db.Shelters.FirstOrDefaultAsync(s => s.email == loginRequest.email);
            if (shelter == null)
                return Unauthorized("Incorrect email or password.");

            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(loginRequest.password, shelter.phone); // Временно! Настрой пароль хранение
            if (!isPasswordCorrect)
                return Unauthorized("Incorrect email or password.");

            string token = _jwtService.GenerateToken(shelter.shelter_owner_id, "shelter");

            return Ok(new
            {
                token = token,
                id = shelter.id,
                name = shelter.name,
                email = shelter.email,
                role = "shelter"
            });
        }

        bool isUserPasswordCorrect = BCrypt.Net.BCrypt.Verify(loginRequest.password, user.password);
        if (!isUserPasswordCorrect)
            return Unauthorized("Incorrect email or password.");

        string userToken = _jwtService.GenerateToken(user.id, user.role ?? "user");

        return Ok(new
        {
            token = userToken,
            id = user.id,
            name = user.name,
            email = user.email,
            role = user.role
        });
    }
}
