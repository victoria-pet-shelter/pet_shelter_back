using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("admin/species")]
public class SpeciesController : ControllerBase
{
    private readonly WikidataFetcher _wikidataFetcher;
    private readonly ILogger<SpeciesController> _logger;
    private readonly string _filePath = Path.Combine(AppContext.BaseDirectory, "Species_Breeds.json");

    public SpeciesController(WikidataFetcher wikidataFetcher, ILogger<SpeciesController> logger)
    {
        _wikidataFetcher = wikidataFetcher;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            if (!System.IO.File.Exists(_filePath))
                return NotFound("Species_Breeds.json not found.");

            var json = System.IO.File.ReadAllText(_filePath);
            var data = JsonConvert.DeserializeObject<List<SpeciesBreedEntry>>(json);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Species_Breeds.json");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{species}")]
    public IActionResult GetBySpecies(string species)
    {
        try
        {
            if (!System.IO.File.Exists(_filePath))
                return NotFound("Species_Breeds.json not found.");

            var json = System.IO.File.ReadAllText(_filePath);
            var data = JsonConvert.DeserializeObject<List<SpeciesBreedEntry>>(json);
            var filtered = data?.Where(x => string.Equals(x.species, species, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Species_Breeds.json by species");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("update")]
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

    public class SpeciesBreedEntry
    {
        public int id { get; set; }
        public string species { get; set; } = string.Empty;
        public string breed { get; set; } = string.Empty;
    }
}
