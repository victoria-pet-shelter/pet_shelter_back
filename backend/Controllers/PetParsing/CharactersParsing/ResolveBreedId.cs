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
            var json = File.ReadAllText("Species_Breeds.json");
            _speciesMap = JsonSerializer.Deserialize<Dictionary<string, SpeciesEntry>>(json)
                          ?? new Dictionary<string, SpeciesEntry>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to load Species_Breeds.json: {ex.Message}");
            _speciesMap = new Dictionary<string, SpeciesEntry>();
        }
    }

    public async Task<int> ResolveBreedIdAsync(string? breedText)
    {
        if (string.IsNullOrWhiteSpace(breedText))
        {
            Console.WriteLine("⚠️ breedText is empty. Defaulting to ID 1.");
            return 1;
        }

        var lower = breedText.ToLower().Trim();

        // Check if breed already exists in DB
        var breed = await _db.Breeds.FirstOrDefaultAsync(b => b.name.ToLower() == lower);
        if (breed != null)
        {
            return breed.id;
        }

        // Try to determine species_id from JSON map
        int speciesId = 1; // fallback default
        foreach (var pair in _speciesMap)
        {
            if (pair.Value.breeds.Exists(b => string.Equals(b, lower, StringComparison.OrdinalIgnoreCase)))
            {
                speciesId = pair.Value.species_id;
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

    private class SpeciesEntry
    {
        public int species_id { get; set; }
        public List<string> breeds { get; set; } = new();
    }
}
