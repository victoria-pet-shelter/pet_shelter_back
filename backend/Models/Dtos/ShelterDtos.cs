namespace Dtos;

public class ShelterCreateDto
{
    public string? name { get; set; }
    public string? address { get; set; }
    public string? phone { get; set; }
    public string? email { get; set; }
    public string? description { get; set; }
}

public class ShelterUpdateDto : ShelterCreateDto {}