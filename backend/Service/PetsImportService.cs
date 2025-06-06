using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using Models;
using System;
using System.Collections.Generic;

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
                var systemUser = await EnsureSystemUserAsync(db);
                var systemShelter = await EnsureSystemShelterAsync(db, systemUser.id);

                var parsedPets = await parser.ParseFromSsLvAsync(systemShelter.id);

                // Filter out duplicates before saving
                List<Pets> newPets = new();
                HashSet<string> urlsInBatch = new();
                
                foreach (var pet in parsedPets)
                {
                    if (string.IsNullOrWhiteSpace(pet.external_url))
                    {
                        continue;
                    }
                    if (urlsInBatch.Contains(pet.external_url))
                    {
                        continue;
                    }

                    bool exists = await db.Pets.AnyAsync(x => x.external_url == pet.external_url);
                    if (!exists)
                        newPets.Add(pet);
                }

                using var transaction = await db.Database.BeginTransactionAsync();
                await db.Pets.AddRangeAsync(newPets);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"✅ Imported {newPets.Count} new pets at {DateTime.Now}");
                int totalPets = await db.Pets.CountAsync();
                Console.WriteLine($"📊 Total pets in database: {totalPets}");

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during import: {ex}");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task<Users> EnsureSystemUserAsync(AppDbContext db)
    {
        string? encryptedEmail = EncryptionService.Encrypt("ss@parser.local");
        var user = await db.Users.FirstOrDefaultAsync(u => u.email == encryptedEmail);

        if (user != null)
            return user;

        var newUser = new Users
        {
            id = Guid.NewGuid(),
            name = "ss.lv parser",
            email = encryptedEmail,
            password = "",
            role = "shelter_owner"
        };

        using var transaction = await db.Database.BeginTransactionAsync();
        await db.Users.AddAsync(newUser);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return newUser;
    }

    private async Task<Shelters> EnsureSystemShelterAsync(AppDbContext db, Guid userId)
    {
        string? encryptedEmail = EncryptionService.Encrypt("ss@parser.local");
        var shelter = await db.Shelters.FirstOrDefaultAsync(s => s.email == encryptedEmail);

        if (shelter != null)
            return shelter;

        var newShelter = new Shelters
        {
            id = Guid.NewGuid(),
            shelter_owner_id = userId,
            name = "Imported from ss.lv",
            email = encryptedEmail,
            address = "internet",
            phone = "0000",
            description = "Dates from website",
            created_at = DateTime.UtcNow
        };

        using var transaction = await db.Database.BeginTransactionAsync();
        await db.Shelters.AddAsync(newShelter);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return newShelter;
    }
}
