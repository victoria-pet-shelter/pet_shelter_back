using Net = Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Json = System.Text.Json;
using Db = Microsoft.EntityFrameworkCore;

namespace Pet_Shelter.Controllers;

[Net.ApiController]
[Net.Route("api/[controller]")]
public class LoginController : Net.ControllerBase
{
    private readonly AppDbContext _dbContext;

    public LoginController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Net.HttpPost]
    public async Task<Net.IActionResult> Login([Net.FromBody] LoginRequest loginRequest)
    {
        if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        try
        {
            // Ищем пользователя по email
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
            if (user == null || !VerifyPassword(loginRequest.Password, user.Password)) // Проверяем пароль
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // Генерация токена сессии (в реальном проекте лучше использовать JWT)
            var sessionId = Guid.NewGuid().ToString();

            // Устанавливаем cookie
            Response.Cookies.Append("session_id", sessionId, new Net.CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Использовать только через HTTPS
                SameSite = Net.SameSiteMode.Strict, // Защита от CSRF
                Path = "/"
            });

            var responseData = new { message = "Login successful", userId = user.Id };
            return Ok(responseData);
        }
        catch (Json.JsonException)
        {
            return BadRequest(new { message = "Invalid JSON format." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error.", error = ex.Message });
        }
    }

    // Метод для проверки пароля (в реальном проекте используйте хэширование)
    private bool VerifyPassword(string inputPassword, string storedPassword)
    {
        // Здесь можно добавить логику хэширования и сравнения
        return inputPassword == storedPassword;
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}