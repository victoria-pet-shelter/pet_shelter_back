using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using Models;
using System;

public class PetImportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PetImportBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var parser = scope.ServiceProvider.GetRequiredService<PetParser>();

            try
            {
                // Find for import
                var systemUser = await EnsureSystemUserAsync(db);
                var systemShelter = await EnsureSystemShelterAsync(db, systemUser.id);

                // Parsing
                var pets = await parser.ParseFromSsLvAsync(systemShelter.id);

                await db.Pets.AddRangeAsync(pets);
                await db.SaveChangesAsync();

                Console.WriteLine($"✅ Imported {pets.Count} pets at {DateTime.Now}");
                int totalPets = await db.Pets.CountAsync();
                Console.WriteLine($"✅ Total: [{totalPets}] pets in database.");
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during import: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken); // every N minutes
        }
    }

    private async Task<Users> EnsureSystemUserAsync(AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.email == "ss@parser.local");
        if (user != null) return user;

        var newUser = new Users
        {
            id = Guid.NewGuid(),
            name = "ss.lv parser",
            email = "ss@parser.local",
            password = "",
            role = "shelter_owner"
        };
        await db.Users.AddAsync(newUser);
        await db.SaveChangesAsync();
        return newUser;
    }

    private async Task<Shelters> EnsureSystemShelterAsync(AppDbContext db, Guid userId)
    {
        var shelter = await db.Shelters.FirstOrDefaultAsync(s => s.email == "ss@parser.local");
        if (shelter != null) return shelter;

        var newShelter = new Shelters
        {
            id = Guid.NewGuid(),
            shelter_owner_id = userId,
            name = "Imported from ss.lv",
            email = "ss@parser.local",
            address = "internet",
            phone = "0000",
            description = "Dates from website",
            created_at = DateTime.UtcNow
        };

        await db.Shelters.AddAsync(newShelter);
        await db.SaveChangesAsync();
        return newShelter;
    }
}
