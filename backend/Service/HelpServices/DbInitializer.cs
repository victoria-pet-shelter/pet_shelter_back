using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Models;
using System;

public static class DbInitializer
{
    private static readonly List<Species> RequiredSpecies = new()
    {
        new Species { id = 1, name = "Dogs" },
        new Species { id = 2, name = "Cats" },
        new Species { id = 3, name = "Exotic" },
        new Species { id = 4, name = "Rodent" },
        new Species { id = 5, name = "Bird" },
        new Species { id = 6, name = "Fish" },
        new Species { id = 7, name = "Farm" },
        new Species { id = 8, name = "Reptile" },
        new Species { id = 9, name = "Unknown" }
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
