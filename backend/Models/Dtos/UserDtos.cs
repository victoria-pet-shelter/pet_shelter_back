// Data Transfer Objects (DTOs) for User
namespace Dtos;

public class UserRegisterDto
{
    public string? name { get; set; }
    public string? email { get; set; }
    public string? password { get; set; }
    public string? role { get; set; }  // "user" или "shelter_owner"
}

public class UserLoginDto
{
    public string? email { get; set; }
    public string? password { get; set; }
}
