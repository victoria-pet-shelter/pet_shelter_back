using Dtos;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Validation;

public class ShelterValidator : IValidator<ShelterCreateDto>
{
    public Dictionary<string, string> Validate(ShelterCreateDto dto)
    {
        Dictionary<string, string> errors = new Dictionary<string, string>();

        // Name
        if (string.IsNullOrWhiteSpace(dto.name))
            errors["name"] = "Name is required.";

        else if (dto.name.Length < 3 || dto.name.Length > 50)
            errors["name"] = "Name must be between 3 and 50 characters.";

        // Address
        if (string.IsNullOrWhiteSpace(dto.address))
            errors["address"] = "Address is required.";

        // Phone
        if (string.IsNullOrWhiteSpace(dto.phone))
            errors["phone"] = "Phone is required.";

        else if (!Regex.IsMatch(dto.phone, @"^[0-9+\-\s()]+$"))
            errors["phone"] = "Phone format is not valid.";

        // Email
        if (string.IsNullOrWhiteSpace(dto.email))
            errors["email"] = "Email is required.";

        else if (!Regex.IsMatch(dto.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            errors["email"] = "Email format is not valid.";

        return errors;
    }
}
