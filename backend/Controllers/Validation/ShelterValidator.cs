using Dtos;

namespace Validation;

public class ShelterValidator : IValidator<ShelterCreateDto>
{
    public Dictionary<string, string> Validate(ShelterCreateDto dto)
    {
        Dictionary<string, string> errors = new Dictionary<string, string>();

        if (dto.name == null || dto.name.Trim() == "")
        {
            errors["name"] = "Name is required.";
        }

        if (dto.address == null || dto.address.Trim() == "")
        {
            errors["address"] = "Address is required.";
        }

        if (dto.phone == null || dto.phone.Trim() == "")
        {
            errors["phone"] = "Phone is required.";
        }

        if (dto.email == null || dto.email.Trim() == "")
        {
            errors["email"] = "Email is required.";
        }

        return errors;
    }
}
