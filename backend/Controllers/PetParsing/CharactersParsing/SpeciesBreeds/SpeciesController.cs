using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;

[ApiController]
[Route("species")]
public class SpeciesController : ControllerBase
{
    private readonly WikidataFetcher _fetcher;

    public SpeciesController(WikidataFetcher fetcher)
    {
        _fetcher = fetcher;
    }

    [HttpGet("update")]
    public async Task<IActionResult> UpdateJson()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "species_breeds.json");
            await _fetcher.UpdateSpeciesBreedsJsonAsync(path);
            return Ok(new { message = "✅ species_breeds.json updated" });
        }
        catch (System.Exception ex)
        {
            return Problem("❌ Error: " + ex.Message);
        }
    }
}

