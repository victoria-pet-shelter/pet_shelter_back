namespace Models;

public class User
{
    public Guid id { get; set; } // UUID
    public string? name { get; set; }
    public string? email { get; set; }
    public string? password { get; set; }
    public string? role { get; set; }  // "admin"/"user"/"shelter_owner"
    public string? phone { get; set; }
    public string? address { get; set; }
}

public class Roles
{
    public int id { get; set; }
    public string? name { get; set; } // admin/user/shelter_owner
}

public class Shelters
{
    public Guid id { get; set; } // UUID
    public Guid shelter_owner_id { get; set; } // UUID пользователя
    public string? name { get; set; }
    public string? address { get; set; }
    public string? phone { get; set; }
    public string? email { get; set; }
    public string? description { get; set; }
    public DateTime created_at { get; set; } // дата создания
}

public class Species
{
    public int id { get; set; }
    public string? name { get; set; } // название вида
}

public class Breeds
{
    public int id { get; set; }
    public int species_id { get; set; } // внешний ключ к таблице species
    public string? name { get; set; } // название породы
}

public class Genders
{
    public int id { get; set; }
    public string? name { get; set; } // пол
}

public class Pets
{
    public Guid id { get; set; } // UUID
    public int species_id { get; set; }
    public int breed_id { get; set; }
    public int gender_id { get; set; }
    public string? name { get; set; }
    public float age { get; set; }
    public string? color { get; set; }
    public string? health { get; set; }
    public int status_id { get; set; }
    public string? mongo_image_id { get; set; }
    public string? description { get; set; }
    public string? image { get; set; }
    public DateTime created_at { get; set; }
    public Guid shelter_id { get; set; } // UUID приюта
}

public class Adoption_statuses
{
    public int id { get; set; }
    public string? name { get; set; }
}

public class Adoption_requests
{
    public Guid id { get; set; }
    public Guid pet_id { get; set; }
    public Guid user_id { get; set; }
    public string? message { get; set; }
    public string? status { get; set; } // "pending", "approved", "rejected"
    public DateTime created_at { get; set; }
}

public class Favorites
{
    public Guid id { get; set; }
    public Guid user_id { get; set; }
    public Guid pet_id { get; set; }
    public DateTime created_at { get; set; }
}

public class News
{
    public Guid id { get; set; }
    public string? title { get; set; }
    public string? content { get; set; }
    public string? image_url { get; set; }
    public DateTime created_at { get; set; }
    public Guid shelter_id { get; set; }
}

public class Reviews
{
    public Guid id { get; set; }
    public Guid user_id { get; set; }
    public Guid shelter_id { get; set; }
    public string? comment { get; set; }
    public int rating { get; set; } // от 1 до 5
    public DateTime created_at { get; set; }
}
