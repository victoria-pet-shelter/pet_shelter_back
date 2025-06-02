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
        ["dogs"] = (@"SELECT ?en ?lv ?ru WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q39367.
            OPTIONAL { ?breed rdfs:label ?en FILTER (lang(?en) = 'en') }
            OPTIONAL { ?breed rdfs:label ?lv FILTER (lang(?lv) = 'lv') }
            OPTIONAL { ?breed rdfs:label ?ru FILTER (lang(?ru) = 'ru') }
        } LIMIT 1000", 1),
        ["cats"] = (@"SELECT ?en ?lv ?ru WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q729.
            OPTIONAL { ?breed rdfs:label ?en FILTER (lang(?en) = 'en') }
            OPTIONAL { ?breed rdfs:label ?lv FILTER (lang(?lv) = 'lv') }
            OPTIONAL { ?breed rdfs:label ?ru FILTER (lang(?ru) = 'ru') }
        } LIMIT 1000", 2),
        ["rabbits"] = (@"SELECT ?en ?lv ?ru WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q7366.
            OPTIONAL { ?breed rdfs:label ?en FILTER (lang(?en) = 'en') }
            OPTIONAL { ?breed rdfs:label ?lv FILTER (lang(?lv) = 'lv') }
            OPTIONAL { ?breed rdfs:label ?ru FILTER (lang(?ru) = 'ru') }
        } LIMIT 1000", 3),
        ["birds"] = (@"SELECT ?en ?lv ?ru WHERE {
            {
                ?breed wdt:P31/wdt:P279* wd:Q512553.
            } UNION {
                ?breed wdt:P31/wdt:P279* wd:Q5113.
            } UNION {
                ?breed wdt:P31/wdt:P279* wd:Q821768.
            }
            OPTIONAL { ?breed rdfs:label ?en FILTER (lang(?en) = 'en') }
            OPTIONAL { ?breed rdfs:label ?lv FILTER (lang(?lv) = 'lv') }
            OPTIONAL { ?breed rdfs:label ?ru FILTER (lang(?ru) = 'ru') }
        } LIMIT 1000", 4),
        ["rodents"] = (@"SELECT ?en ?lv ?ru WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q55983715.
            OPTIONAL { ?breed rdfs:label ?en FILTER (lang(?en) = 'en') }
            OPTIONAL { ?breed rdfs:label ?lv FILTER (lang(?lv) = 'lv') }
            OPTIONAL { ?breed rdfs:label ?ru FILTER (lang(?ru) = 'ru') }
        } LIMIT 1000", 5),
        ["reptiles"] = (@"SELECT ?en ?lv ?ru WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q10884.
            OPTIONAL { ?breed rdfs:label ?en FILTER (lang(?en) = 'en') }
            OPTIONAL { ?breed rdfs:label ?lv FILTER (lang(?lv) = 'lv') }
            OPTIONAL { ?breed rdfs:label ?ru FILTER (lang(?ru) = 'ru') }
        } LIMIT 1000", 6),
        ["horses"] = (@"SELECT ?en ?lv ?ru WHERE {
            ?breed wdt:P31/wdt:P279* wd:Q726.
            OPTIONAL { ?breed rdfs:label ?en FILTER (lang(?en) = 'en') }
            OPTIONAL { ?breed rdfs:label ?lv FILTER (lang(?lv) = 'lv') }
            OPTIONAL { ?breed rdfs:label ?ru FILTER (lang(?ru) = 'ru') }
        } LIMIT 1000", 7),
        ["fish"] = (@"SELECT ?en ?lv ?ru WHERE {
            {
                ?breed wdt:P31/wdt:P279* wd:Q152.
            } UNION {
                ?breed wdt:P31/wdt:P279* wd:Q521682.
            }
            OPTIONAL { ?breed rdfs:label ?en FILTER (lang(?en) = 'en') }
            OPTIONAL { ?breed rdfs:label ?lv FILTER (lang(?lv) = 'lv') }
            OPTIONAL { ?breed rdfs:label ?ru FILTER (lang(?ru) = 'ru') }
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
                    breeds = new List<MultilangBreed>()
                };

                foreach (var result in results.EnumerateArray())
                {
                    string? en = result.TryGetProperty("en", out var enEl) ? enEl.GetProperty("value").GetString() : null;
                    string? lv = result.TryGetProperty("lv", out var lvEl) ? lvEl.GetProperty("value").GetString() : null;
                    string? ru = result.TryGetProperty("ru", out var ruEl) ? ruEl.GetProperty("value").GetString() : null;

                    if (string.IsNullOrWhiteSpace(en) && string.IsNullOrWhiteSpace(lv) && string.IsNullOrWhiteSpace(ru))
                        continue;

                    entry.breeds.Add(new MultilangBreed { en = en, lv = lv, ru = ru });
                }

                speciesMap[species] = entry;
                Console.WriteLine($"✅ Retrieved {entry.breeds.Count} {species} breeds from Wikidata.");
                await Task.Delay(1000);
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

    public class MultilangBreed
    {
        public string? en { get; set; }
        public string? lv { get; set; }
        public string? ru { get; set; }
    }

    public class SpeciesEntry
    {
        public int species_id { get; set; }
        public List<MultilangBreed> breeds { get; set; } = new();
    }
}
