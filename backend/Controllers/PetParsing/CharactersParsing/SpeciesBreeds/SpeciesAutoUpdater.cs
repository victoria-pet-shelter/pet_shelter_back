using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class SpeciesAutoUpdater : BackgroundService
{
    private readonly WikidataFetcher _wikidataFetcher;
    private readonly ILogger<SpeciesAutoUpdater> _logger;

    public SpeciesAutoUpdater(WikidataFetcher wikidataFetcher, ILogger<SpeciesAutoUpdater> logger)
    {
        _wikidataFetcher = wikidataFetcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🔄 Auto-updating species and breeds from Wikidata...");
            await _wikidataFetcher.FetchAndUpdateBreeds();
            _logger.LogInformation("✅ Species_Breeds.json auto-update completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Auto-update of Species_Breeds.json failed.");
        }
    }
}
