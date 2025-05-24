private async Task<int> ResolveBreedIdAsync(string? breedText)
    {
        if (string.IsNullOrWhiteSpace(breedText))
        {
            Console.WriteLine("⚠️ breedText is empty. Defaulting to ID 1.");
            return 1;
        }

        var lower = breedText.ToLower().Trim();
        var breed = await _db.Breeds.FirstOrDefaultAsync(b => b.name.ToLower() == lower);

        if (breed != null)
        {
            return breed.id;
        }

        try
        {
            var newBreed = new Breeds
            {
                name = breedText.Trim(),
                species_id = 1 // 🐶 default
            };

            await _db.Breeds.AddAsync(newBreed);
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Created new breed: {breedText}");
            return newBreed.id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to create breed '{breedText}': {ex.Message}");
            return 1;
        }
    }