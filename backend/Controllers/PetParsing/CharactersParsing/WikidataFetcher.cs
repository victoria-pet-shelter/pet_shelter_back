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

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        using JsonDocument json = JsonDocument.Parse(content);
        var results = json.RootElement.GetProperty("results").GetProperty("bindings");

        foreach (var result in results.EnumerateArray())
        {
            string breed = result.GetProperty("breedLabel").GetProperty("value").GetString();
            string animal = result.TryGetProperty("animalLabel", out var sp)
                ? sp.GetProperty("value").GetString()
                : InferFromType(breed);

            animal = animal.ToLower();

            if (!speciesMap.ContainsKey(animal))
            {
                speciesMap[animal] = new SpeciesEntry
                {
                    species_id = GetSpeciesIdByName(animal),
                    breeds = new List<string>()
                };
            }

            if (!speciesMap[animal].breeds.Contains(breed, StringComparer.OrdinalIgnoreCase))
            {
                speciesMap[animal].breeds.Add(breed);
            }
        }

        var jsonOut = JsonSerializer.Serialize(speciesMap, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, jsonOut);

        Console.WriteLine($"✅ species_breeds.json updated with {speciesMap.Count} species groups.");
    }

    private int GetSpeciesIdByName(string name)
    {
        return name switch
        {
            "dog" or "dogs" or "canine" => 1,
            "cat" or "cats" or "feline" => 2,
            "rabbit" or "hare" or "rabbits" => 3,
            "bird" or "birds" => 4,
            "rodent" or "hamster" or "mouse" or "rat" => 5,
            "reptile" or "snake" or "lizard" or "turtle" => 6,
            "horse" or "pony" or "horses" => 7,
            "fish" or "fishes" => 8,
            _ => 0
        };
    }

    private string InferFromType(string breed)
    {
        string lower = breed.ToLower();
        if (lower.Contains("dog") || lower.Contains("canine")) return "dog";
        if (lower.Contains("cat") || lower.Contains("feline")) return "cat";
        if (lower.Contains("rabbit") || lower.Contains("hare")) return "rabbit";
        if (lower.Contains("bird") || lower.Contains("parrot")) return "bird";
        if (lower.Contains("hamster") || lower.Contains("rat") || lower.Contains("mouse")) return "rodent";
        if (lower.Contains("lizard") || lower.Contains("snake") || lower.Contains("turtle")) return "reptile";
        if (lower.Contains("horse") || lower.Contains("pony")) return "horse";
        if (lower.Contains("fish")) return "fish";
        return "unknown";
    }

    public class SpeciesEntry
    {
        [JsonPropertyName("species_id")]
        public int species_id { get; set; }

        [JsonPropertyName("breeds")]
        public List<string> breeds { get; set; }
    }
} 
