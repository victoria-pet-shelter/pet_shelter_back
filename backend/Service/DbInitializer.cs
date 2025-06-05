using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public static class DbInitializer
{
    private static readonly List<Species> RequiredSpecies = new()
    {
        new Species { id = 1, name = "Dogs" },
        new Species { id = 2, name = "Cats" },
        new Species { id = 3, name = "Rabbits" },
        new Species { id = 4, name = "Birds" },
        new Species { id = 5, name = "Rodents" },
        new Species { id = 6, name = "Reptiles" },
        new Species { id = 7, name = "Horses" },
        new Species { id = 8, name = "Fish" },
        new Species { id = 9, name = "Agricultural" },
        new Species { id = 999, name = "Unknown" }
    };

    public static async Task EnsureDbIsInitializedAsync(AppDbContext db)
    {
        Console.WriteLine("🔍 Checking species...");

        foreach (var species in RequiredSpecies)
        {
            var exists = await db.Species.AnyAsync(s => s.id == species.id);
            if (!exists)
            {
                db.Species.Add(species);
                Console.WriteLine($"➕ Added missing species: {species.name} (id={species.id})");
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine("✅ Species check complete.");
    }
}
