using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Config;

static class AppSettings
{
    public static void ConfigureAppSettings()
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // корень проекта
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Connection string is not set in appsettings.json.");
                return;
            }
            else
            {
                Console.WriteLine("Connection string is set: \n" + connectionString);
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("An error occurred while reading the connection string: " + ex.Message);
            Console.WriteLine("Please check your appsettings.json file and ensure the connection string is correctly configured.");
            throw;
        }
    }
    
    var builder = WebApplication.CreateBuilder(args);

    // Configure Kestrel server to listen on port 80
    // This is where our application will run
    builder.WebHost.ConfigureKestrel(serverOptions =>
        serverOptions.ListenAnyIP(80));

    // Add basic services needed by our application
    // These services are required for ASP.NET Core to work
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure CORS to allow requests from any origin
    // This is needed for frontend applications to call our API
    // It allows any website to make requests to our API
    builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()    // Allow requests from any website
            .AllowAnyMethod()    // Allow any HTTP method (GET, POST, etc.)
            .AllowAnyHeader())); // Allow any HTTP headers
    // Build the application
    // This creates our web application instance
    var app = builder.Build();

    // Configure development environment settings
    // These settings are only active when running in Development mode
    if (app.Environment.IsDevelopment())
    {
        // Enable Swagger UI for API documentation
        // This provides a web interface to test our API endpoints
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Configure middleware
    // These handle incoming HTTP requests in the specified order
    app.UseHttpsRedirection();     // Redirect HTTP requests to HTTPS
    app.UseCors("AllowAll");       // Apply CORS policy
    app.UseAuthorization();        // Handle authentication/authorization

    // Map our API controllers
    // This tells ASP.NET Core where to find our API endpoints
    app.MapControllers();
}
    await app.RunAsync();