using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Models;

public class SpeciesAutoUpdater : BackgroundService
{
    private readonly WikidataFetcher _wikidataFetcher;
    private readonly ILogger<SpeciesAutoUpdater> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public SpeciesAutoUpdater(WikidataFetcher wikidataFetcher, ILogger<SpeciesAutoUpdater> logger, IServiceScopeFactory scopeFactory)
    {
        _wikidataFetcher = wikidataFetcher;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 Auto-updating species and breeds from Wikidata...");

                string path = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "species_breeds.json");

                if (!File.Exists(path))
                {
                    _logger.LogWarning($"species_breeds.json not found at {path}. Creating from Wikidata...");
                }

                await _wikidataFetcher.UpdateSpeciesBreedsJsonAsync(path);
                _logger.LogInformation("✅ species_breeds.json updated.");

                string json = await File.ReadAllTextAsync(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("⚠️ species_breeds.json is empty. Skipping update.");
                }
                else
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, WikidataFetcher.SpeciesEntry>>(json);
                    if (dict == null)
                    {
                        _logger.LogWarning("⚠️ species_breeds.json is invalid. Skipping update.");
                    }
                    else
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        foreach (var entry in dict)
                        {
                            string speciesName = entry.Key;
                            var speciesData = entry.Value;

                            if (speciesData == null || speciesData.breeds == null)
                            {
                                _logger.LogWarning($"⚠️ Invalid or null entry for species '{speciesName}'");
                                continue;
                            }

                            int speciesId = speciesData.species_id;

                            bool speciesExists = await db.Species.AnyAsync(s => s.id == speciesId);
                            if (!speciesExists)
                            {
                                await db.Species.AddAsync(new Species
                                {
                                    id = speciesId,
                                    name = speciesName
                                });
                                _logger.LogInformation($"✅ Inserted species: {speciesName} (id={speciesId})");
                            }

                            foreach (var breed in speciesData.breeds)
                            {
                                if (breed == null || (string.IsNullOrWhiteSpace(breed.en) && string.IsNullOrWhiteSpace(breed.lv) && string.IsNullOrWhiteSpace(breed.ru)))
                                    continue;

                                string primaryName = breed.en ?? breed.lv ?? breed.ru ?? "Unknown";

                                bool breedExists = await db.Breeds.AnyAsync(b =>
                                    b.name_en == breed.en &&
                                    b.name_lv == breed.lv &&
                                    b.name_ru == breed.ru
                                );

                                if (!breedExists)
                                {
                                    await db.Breeds.AddAsync(new Breeds
                                    {
                                        name = primaryName,
                                        name_en = breed.en,
                                        name_lv = breed.lv,
                                        name_ru = breed.ru,
                                        species_id = speciesId
                                    });
                                    _logger.LogInformation($"✅ Inserted breed: {primaryName} (en='{breed.en}', lv='{breed.lv}', ru='{breed.ru}') for species {speciesName}");
                                }
                            }
                        }

                        await db.SaveChangesAsync();
                        _logger.LogInformation("✅ Species and Breeds tables updated.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Auto-update of Species and Breeds failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
