using Validation;
using Dtos;

namespace Validation;

public class UserLoginValidator : IValidator<UserLoginDto>
{
    public Dictionary<string, string> Validate(UserLoginDto login)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(login.email))
            errors["email"] = "Email is required.";

        if (string.IsNullOrWhiteSpace(login.password))
            errors["password"] = "Password is required.";

        return errors;
    }
}
