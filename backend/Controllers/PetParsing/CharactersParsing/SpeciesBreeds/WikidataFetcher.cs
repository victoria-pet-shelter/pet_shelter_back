using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class WikidataFetcher
{
    private static readonly HttpClient _httpClient;
    private const string Endpoint = "https://query.wikidata.org/sparql";

    static WikidataFetcher()
    {
        var handler = new HttpClientHandler();
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; PetShelterBot/1.0)");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/sparql-results+json");
    }

    private static readonly Dictionary<string, (string Query, int SpeciesId)> SpeciesQueries = new()
    {
        ["dogs"] = (@"SELECT ?breedLabel WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q39367.
            SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 1000", 1),

        ["cats"] = (@"SELECT ?breedLabel WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q1360758.
            SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 1000", 2),

        ["rabbits"] = (@"SELECT ?breedLabel WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q66218453.
            SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 1000", 3),

        ["birds"] = (@"SELECT ?breedLabel WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q56893223.
            SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 1000", 4),

        ["rodents"] = (@"SELECT ?breedLabel WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q55983715.
            SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 1000", 5),

        ["reptiles"] = (@"SELECT ?breedLabel WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q310892.
            SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 1000", 6),

        ["horses"] = (@"SELECT ?breedLabel WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q634802.
            SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 1000", 7),

        ["fish"] = (@"SELECT ?breedLabel WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q28885052.
            SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 1000", 8)
    };

    public async Task UpdateSpeciesBreedsJsonAsync(string outputPath)
    {
        var speciesMap = new Dictionary<string, SpeciesEntry>();

        foreach (var (species, data) in SpeciesQueries)
        {
            try
            {
                string query = data.Query;
                int speciesId = data.SpeciesId;

                var url = Endpoint + "?query=" + Uri.EscapeDataString(query);
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                using JsonDocument json = JsonDocument.Parse(content);
                var results = json.RootElement.GetProperty("results").GetProperty("bindings");

                var entry = new SpeciesEntry
                {
                    species_id = speciesId,
                    breeds = new List<string>()
                };

                foreach (var result in results.EnumerateArray())
                {
                    if (result.TryGetProperty("breedLabel", out var labelElem))
                    {
                        string? label = labelElem.GetProperty("value").GetString();

                        if (!string.IsNullOrWhiteSpace(label) &&
                            !label.StartsWith("Q") &&
                            !entry.breeds.Contains(label, StringComparer.OrdinalIgnoreCase))
                        {
                            entry.breeds.Add(label);
                        }
                    }
                }

                speciesMap[species] = entry;
                Console.WriteLine($"✅ Retrieved {entry.breeds.Count} {species} breeds from Wikidata.");
                await Task.Delay(1000); // чтобы не словить бан
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to fetch breeds for {species}: {ex.Message}");
            }
        }

        string output = JsonSerializer.Serialize(speciesMap, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, output);
        Console.WriteLine($"✅ species_breeds.json updated with {speciesMap.Count} species groups.");
    }

    public class SpeciesEntry
    {
        [JsonPropertyName("species_id")]
        public int species_id { get; set; }

        [JsonPropertyName("breeds")]
        public List<string> breeds { get; set; } = new();
    }
}
