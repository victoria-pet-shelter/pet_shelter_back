using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;

public class BreedResolver
{
    private readonly AppDbContext _db;
    private readonly SpeciesDetector _detector;

    public BreedResolver(AppDbContext db)
    {
        string breedsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "species_breeds.json");
        string keywordsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "species_keywords.json");
        string fallbackLogPath = Path.Combine(AppContext.BaseDirectory, "Logs", "unknown_breeds.log");

        _db = db;
        _detector = new SpeciesDetector(breedsPath, keywordsPath, fallbackLogPath);
    }

    public async Task<int> ResolveBreedIdAsync(string? breedName)
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

            int? speciesId = _detector.DetectSpeciesId(breedName);

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
