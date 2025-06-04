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

    public async Task<int> ResolveBreedIdAsync(MultilangBreed breedData)
    {
        string? primaryName = breedData.en ?? breedData.lv ?? breedData.ru;

        if (string.IsNullOrWhiteSpace(primaryName))
        {
            return 1;
        }

        var lower = primaryName.ToLowerInvariant().Trim();

        var existing = await _db.Breeds.FirstOrDefaultAsync(b =>
            b.name_en == breedData.en &&
            b.name_lv == breedData.lv &&
            b.name_ru == breedData.ru
        );

        if (existing != null)
        {
            return existing.id;
        }

        int speciesId = 999;
        foreach (var pair in _speciesMap)
        {
            var entry = pair.Value;
            if (entry != null && entry.breeds != null &&
                entry.breeds.Exists(b =>
                    b.en?.ToLower() == lower ||
                    b.lv?.ToLower() == lower ||
                    b.ru?.ToLower() == lower))
            {
                speciesId = entry.species_id;
                break;
            }
        }

        try
        {
            var newBreed = new Breeds
            {
                name = primaryName,
                name_en = breedData.en,
                name_lv = breedData.lv,
                name_ru = breedData.ru,
                species_id = speciesId
            };

            await _db.Breeds.AddAsync(newBreed);
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Created new breed: {primaryName} (species_id: {speciesId})");
            return newBreed.id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to create breed '{primaryName}': {ex.Message}");
            return 1;
        }
    }
}
