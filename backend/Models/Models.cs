namespace Models;

public class Users
{
    public Guid id { get; set; } // UUID
    public string? name { get; set; }
    public string? email { get; set; }
    public string? password { get; set; }
    public string? role { get; set; }  // "admin"/"user"/"shelter"
    public string? phone { get; set; }
    public string? address { get; set; }

    // One user can own multiple shelters
    public List<Shelters>? Shelters { get; set; }
}

public class Roles
{
    public int id { get; set; }
    public string? name { get; set; } // admin/user/shelter
}

public class Shelters
{
    public Guid id { get; set; } // UUID
    public Guid shelter_owner_id { get; set; } // UUID of the user

    public string? name { get; set; }
    public string? address { get; set; }
    public string? phone { get; set; }
    public string? email { get; set; }
    public string? description { get; set; }
    public DateTime created_at { get; set; } // creation date

    // Navigation property to owner
    public Users? Owner { get; set; }
}

public class Species
{
    public int id { get; set; }
    public string? name { get; set; } // species name
}

public class Breeds
{
    public int id { get; set; }
    public int species_id { get; set; } // foreign key to species
    public string? name { get; set; } // breed name
}

public class Genders
{
    public int id { get; set; }
    public string? name { get; set; } // gender
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
    public Guid shelter_id { get; set; } // UUID of shelter

    // Navigation properties
    public Shelters? Shelter { get; set; }
    public Species? Species { get; set; }
    public Breeds? Breed { get; set; }
    public Genders? Gender { get; set; }
}

public class AdoptionStatuses
{
    public int id { get; set; }
    public string? name { get; set; }
}

public class AdoptionRequests
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
    public int rating { get; set; } // 1 to 5
    public DateTime created_at { get; set; }
}
