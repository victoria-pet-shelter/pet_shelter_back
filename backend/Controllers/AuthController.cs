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

    public AuthController(AppDbContext db, JwtService jwtService)
    {
        this.db = db;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto user)
    {
        var validator = new UserRegisterValidator();
        var errors = validator.Validate(user);

        if (errors.Any())
            return BadRequest(new { errors });

        try
        { // Encrypt
            string encryptedEmail = EncryptionService.Encrypt(user.email);

            if (await db.Users.AnyAsync(u => u.name == user.name))
                return BadRequest("Username already exists.");

            if (await db.Users.AnyAsync(u => u.email == encryptedEmail))
                return BadRequest("Email already exists.");

            var newUser = new Users
            {
                id = Guid.NewGuid(),
                name = user.name,
                email = encryptedEmail,// Encrypt
                password = BCrypt.Net.BCrypt.HashPassword(user.password), // Encrypt
                role = user.role
            };

            using var transaction = await db.Database.BeginTransactionAsync();
            await db.Users.AddAsync(newUser);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

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
        {   // Encrypt
            string encryptedEmail = EncryptionService.Encrypt(loginRequest.email);

            var user = await db.Users.FirstOrDefaultAsync(u => u.email == encryptedEmail);
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
                email = EncryptionService.Decrypt(user.email),// Encrypt
                phone = user.phone != null ? EncryptionService.Decrypt(user.phone) : null, // Encrypt
                role = user.role
            });
        }
        catch (Exception ex)
        {
            return Problem("Error: " + ex.Message);
        }
    }

}