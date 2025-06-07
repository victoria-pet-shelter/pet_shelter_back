using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Models;
using System;

public class BreedResolver
{
    private readonly AppDbContext _db;
    
    public BreedResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> ResolveBreedIdAsync(string? breedName, int speciesId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(breedName))
            {
                return await GetOrCreateBreedAsync("Unknown", null);
            }

            string normalized = breedName.ToLower().Trim();

            var existing = await _db.Breeds
                .Where(b => b.name != null && b.name.ToLower() == normalized)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return existing.id;
            }

            return await GetOrCreateBreedAsync(breedName!, speciesId);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Breed resolution error: " + ex.Message);
            return await GetOrCreateBreedAsync("Unknown", null);
        }
    }

    private async Task<int> GetOrCreateBreedAsync(string name, int? speciesId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unknown";
        }

        var newBreed = new Breeds
        {
            name = name,
            species_id = speciesId ?? 0 // default fallback to 0 if speciesId is null
        };

        _db.Breeds.Add(newBreed);
        await _db.SaveChangesAsync();
        return newBreed.id;
    }
}
