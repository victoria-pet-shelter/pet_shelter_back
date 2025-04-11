using ORM = Microsoft.EntityFrameworkCore;
using System.Net;

namespace Pet_Shelter
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // корень проекта
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>(); // загрузка секции JwtSettings

            Console.WriteLine("JWT Secret Key: " + jwtSettings.SecretKey);
        }
        
        
    }
}
