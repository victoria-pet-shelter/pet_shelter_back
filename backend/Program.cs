using Db = Microsoft.EntityFrameworkCore;
using Net = Microsoft.AspNetCore;
using Config;

namespace Pet_Shelter
{
    class Program
    {
        static void Main(string[] args)
        {
            AppSettings.ConfigureAppSettings();
            builder.Services.ConfigureAppSettings(builder.Configuration);
        }
    }
}
