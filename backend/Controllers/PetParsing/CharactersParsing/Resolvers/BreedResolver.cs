using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Models;

public class BreedResolver
{
    private readonly AppDbContext _db;
    private readonly Dictionary<string, SpeciesEntry> _speciesMap;

    public BreedResolver(AppDbContext db)
    {
        _db = db;

        try
        {
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "species_breeds.json");
            var json = File.ReadAllText(jsonPath);
            _speciesMap = JsonSerializer.Deserialize<Dictionary<string, SpeciesEntry>>(json)
                          ?? new Dictionary<string, SpeciesEntry>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to load species_breeds.json: {ex.Message}");
            _speciesMap = new Dictionary<string, SpeciesEntry>();
        }
    }

    public async Task<int> ResolveBreedIdAsync(string? breedText)
    {
        if (string.IsNullOrWhiteSpace(breedText))
        {
            return 1;
        }

        var lower = breedText.ToLowerInvariant().Trim();

        var breed = await _db.Breeds.FirstOrDefaultAsync(b => b.name.ToLower() == lower);
        if (breed != null)
        {
            return breed.id;
        }

        int speciesId = 999;
        foreach (var pair in _speciesMap)
        {
            var entry = pair.Value;
            if (entry != null && entry.breeds != null &&
                entry.breeds.Exists(b => string.Equals(b, lower, StringComparison.OrdinalIgnoreCase)))
            {
                speciesId = entry.species_id;
                break;
            }
        }

        try
        {
            var newBreed = new Breeds
            {
                name = breedText.Trim(),
                species_id = speciesId
            };

            await _db.Breeds.AddAsync(newBreed);
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Created new breed: {breedText} (species_id: {speciesId})");
            return newBreed.id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to create breed '{breedText}': {ex.Message}");
            return 1;
        }
    }
}