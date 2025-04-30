using Microsoft.Extensions.Configuration;

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
    
}
