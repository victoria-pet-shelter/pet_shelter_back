using Dtos;

namespace Validation;

public class UserRegisterValidator : IValidator<UserRegisterDto>
{
    public Dictionary<string, string> Validate(UserRegisterDto user)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(user.email))
            errors["email"] = "Email is required.";
        else
        {
            if (user.email.Length < 5 || user.email.Length > 50)
                errors["email"] = "Email must be between 5 and 50 characters.";
            else if (!Regex.IsMatch(user.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors["email"] = "Email is not valid.";
        }

        if (string.IsNullOrWhiteSpace(user.password))
            errors["password"] = "Password is required.";
        else
        {
            if (user.password.Length < 6 || user.password.Length > 20)
                errors["password"] = "Password must be between 6 and 20 characters.";
            else if (!Regex.IsMatch(user.password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
                errors["password"] = "Password must contain at least one uppercase letter, one lowercase letter, and one number.";
        }

        if (string.IsNullOrWhiteSpace(user.name))
            errors["name"] = "Username is required.";
        else
        {
            if (user.name.Length < 3 || user.name.Length > 20)
                errors["name"] = "Username must be between 3 and 20 characters.";
            else if (!Regex.IsMatch(user.name, @"^[a-zA-Z0-9]+$"))
                errors["name"] = "Username must contain only letters and numbers.";
        }

        if (string.IsNullOrEmpty(user.role) || (user.role != "user" && user.role != "shelter_owner"))
            errors["role"] = "Role must be either 'user' or 'shelter_owner'.";

        return errors;
    }
}
