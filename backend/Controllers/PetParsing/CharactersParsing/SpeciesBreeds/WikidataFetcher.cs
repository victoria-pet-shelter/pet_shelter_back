using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class WikidataFetcher
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string Endpoint = "https://query.wikidata.org/sparql";
    private Dictionary<string, List<string>> _speciesKeywords;

    public WikidataFetcher()
    {
        try
        {
            var keywordsJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "species_keywords.json"));
            _speciesKeywords = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(keywordsJson)
                ?? new Dictionary<string, List<string>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Failed to load species_keywords.json: " + ex.Message);
            _speciesKeywords = new Dictionary<string, List<string>>();
        }
    }

    public async Task UpdateSpeciesBreedsJsonAsync(string outputPath)
    {
        var speciesMap = new Dictionary<string, SpeciesEntry>();

        var sparqlQuery = @"SELECT ?breedLabel ?animalLabel WHERE {
          ?breed wdt:P31 ?type.
          VALUES ?type { wd:Q39367 wd:Q1360758 wd:Q66218453 wd:Q56893223 wd:Q55983715 wd:Q310892 wd:Q634802 wd:Q28885052 wd:Q20747295 wd:Q25269 }
          OPTIONAL { ?breed wdt:P279 ?animal. }
          SERVICE wikibase:label { bd:serviceParam wikibase:language 'en,lv,ru'. }
        } LIMIT 500";

        var url = Endpoint + "?query=" + Uri.EscapeDataString(sparqlQuery);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/sparql-results+json");
        request.Headers.Add("User-Agent", "PetShelterBot/1.0 (https://example.com/contact)");

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine("🔍 Wikidata response content:"); // for test
        Console.WriteLine(content); // for test

        using JsonDocument json = JsonDocument.Parse(content);
        var results = json.RootElement.GetProperty("results").GetProperty("bindings");

        foreach (var result in results.EnumerateArray())
        {
            if (!result.TryGetProperty("breedLabel", out var breedLabelElement)) continue;

            string? breed = breedLabelElement.GetProperty("value").GetString();
            if (string.IsNullOrWhiteSpace(breed)) continue;

            string animal = result.TryGetProperty("animalLabel", out var sp)
                ? sp.GetProperty("value").GetString() ?? InferSpeciesName(breed)
                : InferSpeciesName(breed);

            animal = animal.ToLower();
            int speciesId = InferSpeciesId(breed);

            if (!speciesMap.ContainsKey(animal))
            {
                speciesMap[animal] = new SpeciesEntry
                {
                    species_id = speciesId,
                    breeds = new List<string>()
                };
            }

            if (!speciesMap[animal].breeds.Contains(breed, StringComparer.OrdinalIgnoreCase))
            {
                speciesMap[animal].breeds.Add(breed);
            }
        }

        var jsonOut = JsonSerializer.Serialize(speciesMap, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("🟨 JSON preview:");
        Console.WriteLine(jsonOut);

        await File.WriteAllTextAsync(outputPath, jsonOut);

        Console.WriteLine($"✅ species_breeds.json updated with {speciesMap.Count} species groups.");
    }

    public int InferSpeciesId(string? breed)
    {
        if (string.IsNullOrWhiteSpace(breed)) return 0;

        string lower = breed.ToLower();
        foreach (var pair in _speciesKeywords)
        {
            foreach (var keyword in pair.Value)
            {
                if (lower.Contains(keyword.ToLower()))
                {
                    return pair.Key switch
                    {
                        "dog" => 1,
                        "cat" => 2,
                        "rabbit" => 3,
                        "bird" => 4,
                        "rodent" => 5,
                        "reptile" => 6,
                        "horse" => 7,
                        "fish" => 8,
                        _ => 0
                    };
                }
            }
        }
        return 0;
    }

    public string InferSpeciesName(string? breed)
    {
        if (string.IsNullOrWhiteSpace(breed)) return "unknown";

        string lower = breed.ToLower();

        // Known labels from Wikidata (animalLabel values)
        Dictionary<string, string> labelToSpecies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "dog", "dogs" },
            { "herding dog", "dogs" },
            { "terrier", "dogs" },
            { "hound", "dogs" },
            { "cattle dog", "dogs" },
            { "shepherd dog", "dogs" },
            { "swiss mountain dogs", "dogs" },
            { "toy dog", "dogs" },
            { "schnauzer", "dogs" },
            { "retriever", "dogs" },
            { "gun dog", "dogs" },
            { "spitz", "dogs" },
            
            { "cat", "cats" },
            { "rabbit", "rabbits" },
            { "bird", "birds" },
            { "rodent", "rodents" },
            { "reptile", "reptiles" },
            { "horse", "horses" },
            { "fish", "fish" }
        };

        // Try mapping directly from breed keyword list
        foreach (var pair in _speciesKeywords)
        {
            foreach (var keyword in pair.Value)
            {
                if (lower.Contains(keyword.ToLower()))
                {
                    return pair.Key;
                }
            }
        }

        // Try mapping from fallback animalLabel
        if (_lastAnimalLabel != null && labelToSpecies.TryGetValue(_lastAnimalLabel.ToLower(), out var mapped))
        {
            return mapped;
        }

        return "unknown";
    }


    public class SpeciesEntry
    {
        [JsonPropertyName("species_id")]
        public int species_id { get; set; }

        [JsonPropertyName("breeds")]
        public List<string> breeds { get; set; } = new();
    }
}
