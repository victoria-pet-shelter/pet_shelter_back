namespace Models;

public class User{
    public int id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public string role { get; set; }  // "admin"/"user"/shelter_owner
    public string phone { get; set; }
    public string address { get; set; }
}

public class Roles{
    public int id { get; set; }
    public string name { get; set; } // "admin"/"user"/shelter_owner
}

public class Shelters{
    public int uuid { get; set; }
    public int id { get; set; } // shelter id shelter_owner
    public string name { get; set; }
    public string address { get; set; }
    public string phone { get; set; }
    public string email { get; set; }
    public string description { get; set; }
    public string created_at { get; set; } // date of creation
}

public class species{
    public int id { get; set; }
    public string name { get; set; } // species name
}

public class breeds{
    public int id { get; set; }
    public int species_id { get; set; } // foreign key to species table
    public string name { get; set; } // breed name
}

public class genders{
    public int id { get; set; }
    public string name { get; set; } // gender name
}

public class pets{
    public int id { get; set; }
    public int species_id { get; set; } // foreign key to species table
    public int breed_id { get; set; } // foreign key to breed table
    public int gender_id { get; set; } // foreign key to gender table
    public string name { get; set; }
    public float age { get; set; } 
    public string color { get; set; }
    public string health { get; set; } // health status
    public int status_id { get; set; } // foreign key to status table
    public string mongo_image_id { get; set; } // image id in MongoDB
    public string description { get; set; }
    public string image { get; set; }
    public string created_at { get; set; } // date of creation
    public int shelter_id { get; set; } // foreign key to shelter table
}

public class adoption_statuses{
    public int id { get; set; }
    public string name { get; set; } // adoption status name
}

public class adoption_requests{
    public int id { get; set; }
    public int pet_id { get; set; } // foreign key to pet table
    public int user_id { get; set; } // foreign key to user table
    public string message { get; set; } // message from user to shelter_owner
    public string status { get; set; } // "pending"/"approved"/"rejected"
    public string created_at { get; set; } // date of creation
}

public class favorites{
    public int id { get; set; }
    public int user_id { get; set; } // foreign key to user table
    public int pet_id { get; set; } // foreign key to pet table
    public string created_at { get; set; } // date of creation
}

public class news{
    public int id { get; set; }
    public string title { get; set; }
    public string content { get; set; }
    public string image_url { get; set; } // image id in MongoDB
    public string created_at { get; set; } // date of creation
    public int shelter_id { get; set; } // foreign key to shelter table
}

public class reviews{
    public int id { get; set; }
    public int user_id { get; set; } // foreign key to user table
    public int shelter_id { get; set; } // foreign key to shelter table
    public string comment { get; set; }
    public int rating { get; set; } // rating from 1 to 5

    public string created_at { get; set; } // date of creation
}