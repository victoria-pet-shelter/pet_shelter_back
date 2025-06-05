using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using MongoDB.Driver;
using ImageFetchers;
using System.Text;
using DotNetEnv;
using Config;
using Models;

Console.OutputEncoding = Encoding.UTF8;
// For Self:
var solutionRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
Env.Load(Path.Combine(solutionRoot, ".env"));
Console.WriteLine("✅ .env downloaded from: " + Path.Combine(solutionRoot, ".env"));

// For Docker:
Env.Load(".env");

var builder = WebApplication.CreateBuilder(args);

try
{
    // Load env vars
    string? dbHost = Environment.GetEnvironmentVariable("DB_HOST");
    string? dbName = Environment.GetEnvironmentVariable("DB_NAME");
    string? dbUser = Environment.GetEnvironmentVariable("DB_USER");
    string? dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    string? mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
    string? jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
    string? jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
    string? jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
    string? encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");

    void Check(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ {name} is missing.");
            Console.ResetColor();
            throw new Exception($"{name} is required.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ {name} loaded.");
            Console.ResetColor();
        }
    }

    // Validate required vars
    Check(dbHost, "DB_HOST");
    Check(dbName, "DB_NAME");
    Check(dbUser, "DB_USER");
    Check(dbPassword, "DB_PASSWORD");
    Check(mongoUri, "MONGO_URI");
    Check(jwtKey, "JWT_KEY");
    Check(jwtIssuer, "JWT_ISSUER");
    Check(jwtAudience, "JWT_AUDIENCE");
    Check(encryptionKey, "ENCRYPTION_KEY");

    // PostgreSQL test
    var pgConn = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword}";
    using (var pg = new Npgsql.NpgsqlConnection(pgConn))
    {
        pg.Open();
        using var cmd = new Npgsql.NpgsqlCommand("SELECT 1", pg);
        cmd.ExecuteScalar();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ PostgreSQL connection OK.");
        Console.ResetColor();
    }

    // MongoDB test
    var mongoClient = new MongoClient(mongoUri);
    var mongoDb = mongoClient.GetDatabase("PetShelterMedia");
    mongoDb.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}").Wait();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ MongoDB connection OK.");
    Console.ResetColor();

    // Register services
    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(pgConn));
    builder.Services.AddSingleton<IMongoClient>(mongoClient);
    builder.Services.AddSingleton(mongoDb);
    Console.WriteLine("✅ Usage:http://localhost:5000 and http://localhost:5000/swagger/");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("❌ Startup error: " + ex.Message);
    Console.ResetColor();
    return;
}

// JWT-service
builder.Services.AddSingleton<JwtService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")!)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    };
});

// Controllers и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pet Shelter API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token. Example: Bearer {your_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Pets Parsing
builder.Services.AddHostedService<PetImportBackgroundService>();
builder.Services.AddScoped<PetParser>();
builder.Services.AddScoped<BreedResolver>();
builder.Services.AddScoped<GenderResolver>();
builder.Services.AddSingleton<MongoService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IServiceScopeFactory>(sp => sp.GetRequiredService<IServiceScopeFactory>());
builder.Services.AddTransient<ImageFetcher>();

string breedsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "species_breeds.json");
string keywordsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "species_keywords.json");
string logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "unknown_breeds.log");
builder.Services.AddSingleton(new SpeciesDetector(breedsPath, keywordsPath, logPath));

// CORS for Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Logging off
builder.Logging.ClearProviders();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto migrate and seed species
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbInitializer.EnsureDbIsInitializedAsync(db);
}

app.Run();
