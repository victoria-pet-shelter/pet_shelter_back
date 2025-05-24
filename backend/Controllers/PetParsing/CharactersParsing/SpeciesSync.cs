using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

[ApiController]
[Route("admin/species")]
public class SpeciesSyncController : ControllerBase
{
    private readonly WikidataFetcher _wikidataFetcher;
    private readonly ILogger<SpeciesSyncController> _logger;

    public SpeciesSyncController(WikidataFetcher wikidataFetcher, ILogger<SpeciesSyncController> logger)
    {
        _wikidataFetcher = wikidataFetcher;
        _logger = logger;
    }

    [HttpPost("update-breeds")]
    public async Task<IActionResult> UpdateBreeds()
    {
        try
        {
            await _wikidataFetcher.FetchAndUpdateBreeds();
            _logger.LogInformation("✅ Species_Breeds.json updated successfully.");
            return Ok(new { message = "Breeds updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to update breeds.");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
