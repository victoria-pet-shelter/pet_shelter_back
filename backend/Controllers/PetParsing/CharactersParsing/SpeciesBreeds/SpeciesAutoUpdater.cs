using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class SpeciesAutoUpdater : BackgroundService
{
    private readonly WikidataFetcher _wikidataFetcher;
    private readonly ILogger<SpeciesAutoUpdater> _logger;
    private readonly IServiceProvider _services;

    public SpeciesAutoUpdater(WikidataFetcher wikidataFetcher, ILogger<SpeciesAutoUpdater> logger, IServiceProvider services)
    {
        _wikidataFetcher = wikidataFetcher;
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🔄 Auto-updating species and breeds from Wikidata...");

            string path = Path.Combine(Directory.GetCurrentDirectory(), "species_breeds.json");
            await _wikidataFetcher.UpdateSpeciesBreedsJsonAsync(path);
            _logger.LogInformation("✅ species_breeds.json updated.");

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string json = await File.ReadAllTextAsync(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, WikidataFetcher.SpeciesEntry>>(json);

            if (dict == null)
            {
                _logger.LogWarning("⚠️ species_breeds.json is empty or invalid.");
                return;
            }

            foreach (var entry in dict)
            {
                string speciesName = entry.Key;
                int speciesId = entry.Value.species_id;

                bool exists = await db.Species.AnyAsync(s => s.id == speciesId);
                if (!exists)
                {
                    await db.Species.AddAsync(new Species
                    {
                        id = speciesId,
                        name = speciesName
                    });
                    _logger.LogInformation($"✅ Inserted species: {speciesName} (id={speciesId})");
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("✅ Species table updated.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Auto-update of Species failed.");
        }

        await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
    }
}
