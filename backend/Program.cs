using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using System.Text;
using DotNetEnv;
using Config;

Console.OutputEncoding = Encoding.UTF8;
Env.Load(Path.Combine(AppContext.BaseDirectory, ".env")); // Load .env

var builder = WebApplication.CreateBuilder(args);

try
{
    // Check environment variables
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

    if (string.IsNullOrWhiteSpace(dbHost) || string.IsNullOrWhiteSpace(dbName) || string.IsNullOrWhiteSpace(dbUser))
        throw new Exception("One or more database environment variables are missing. \nPlease check DB_HOST, DB_NAME, DB_USER, DB_PASSWORD in your .env file.");

    var connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword}";

    // Test database connection
    using (var testConnection = new Npgsql.NpgsqlConnection(connectionString))
    {
        testConnection.Open();
        using var cmd = new Npgsql.NpgsqlCommand("SELECT 1", testConnection);
        cmd.ExecuteScalar();
        Console.WriteLine("✅ Database connection test passed.");
    }

    // Add DbContext with connection string
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
catch (Exception ex)
{
    Console.WriteLine("❌ Error message: " + ex.GetBaseException().Message);
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
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!))
    };
});

// Controllers и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pet Shelter API", Version = "v1" });
});

// Logging closed 
builder.Logging.ClearProviders();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
