# Pete Shelter Backend

Pet Shelter is a web application for pets in shelters, working with applications for the adoption and authorization of users. This is a backend-part on C# using ASP.NET Core and Entity Framework Core.

## Peculiarities:

* JWT AUTORIME
* Registration/input of users
* Accounting for pets, shelters, reviews
* PostgreSQL + Mongodb (in the future)
* Swagger UI for viewing and testing API

---

## Settings `.env`

At the root of the folder `Backend/` `.env` file should lie:

```ENV
DB_HOST = localhost
DB_NAME = pet_shelter
DB_USER = postgres
DB_PASSWORD = your_password

JWT_KEY = your_JWT_SECRET_KEY
JWT_ISSUER = Petshelter
JWT_AUDIENCE = PetShelterClient
JWT_EXPIRE_MINUTES = 60
```

> Replace `your_password` and` your_jwt_secret_key` to your meanings.

---

## Project launch

In the terminal on folder /meta:

```Makefile
make
```

### 4. Swagger UI

Open in the browser:

```
http://localhost:5000/swagger
```

There you can send `post /register` or` post /login` and get a jwt-token.
