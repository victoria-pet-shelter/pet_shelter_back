using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using Models;
using MongoDB.Bson;
using ImageFetchers;

public class PetParser
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MongoService _mongoService;
    private readonly AppDbContext _db;
    private readonly BreedResolver _breedResolver;
    private readonly ImageFetcher _imageFetcher;
    private readonly SpeciesDetector _speciesDetector;
    private readonly string _linkPath;

    public PetParser(
        IHttpClientFactory httpClientFactory,
        MongoService mongoService,
        AppDbContext db,
        BreedResolver breedResolver,
        ImageFetcher imageFetcher,
        SpeciesDetector speciesDetector)
    {
        _httpClientFactory = httpClientFactory;
        _mongoService = mongoService;
        _db = db;
        _breedResolver = breedResolver;
        _imageFetcher = imageFetcher;
        _speciesDetector = speciesDetector;
        _linkPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "SsLvLinks.json");
    }

    public async Task<List<Pets>> ParseFromSsLvAsync(Guid shelterId, int max = 5)
    {
        List<Pets> result = new();
        var client = _httpClientFactory.CreateClient();
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);

        List<string> urls;
        try
        {
            var json = await File.ReadAllTextAsync(_linkPath);
            urls = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to read SsLvLinks.json: {ex.Message}");
            return result;
        }

        foreach (var baseUrl in urls)
        {
            int page = 1;
            int collectedFromCategory = 0;

            while (collectedFromCategory < max)
            {
                string url = baseUrl + (page > 1 ? $"page{page}.html" : "index.html");
                Console.WriteLine($"Parsing: {url}");

                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) break;
                }
                catch
                {
                    Console.WriteLine($"⚠️ Skipped unavailable URL: {url}");
                    break;
                }

                var html = await response.Content.ReadAsStringAsync();
                var document = await context.OpenAsync(req => req.Content(html));
                var ads = document.QuerySelectorAll(".d1").Where(e => e.QuerySelector("a") != null);

                if (!ads.Any()) break;

                foreach (var ad in ads)
                {
                    try
                    {
                        var link = ad.QuerySelector("a")?.GetAttribute("href");
                        if (string.IsNullOrEmpty(link)) continue;
                        var fullLink = "https://www.ss.lv" + link;

                        // if (await _db.Pets.AnyAsync(p => p.external_url == fullLink)) continue;

                        var petPageHtml = await client.GetStringAsync(fullLink);
                        var petDoc = await context.OpenAsync(req => req.Content(petPageHtml));

                        var title = petDoc.QuerySelector("title")?.TextContent?.Trim();
                        var cleanTitle = Regex.Replace(title?.Split("€. ").LastOrDefault() ?? title ?? "Unnamed", @"-+\s*Sludinājumi\s*$", "", RegexOptions.IgnoreCase).Trim();

                        var breedText = GetFieldValue(petDoc, "Šķirne:") ?? GetFieldValue(petDoc, "Порода:");
                        var ageText = GetFieldValue(petDoc, "Vecums:") ?? GetFieldValue(petDoc, "Возраст:");
                        var colorText = GetFieldValue(petDoc, "Krāsa:") ?? GetFieldValue(petDoc, "Окрас:");
                        var description = petDoc.QuerySelector("div[id^='msg_div_msg']")?.TextContent?.Trim();
                        var priceText = PriceResolver.ExtractPrice(description);
                        var photoId = await _imageFetcher.FetchImageIdFromPage(petDoc);

                        int? speciesId = _speciesDetector.DetectSpeciesId(breedText) ?? 999;
                        int breedId = await _breedResolver.ResolveBreedIdAsync(breedText);

                        Uri uri = new Uri(baseUrl);
                        string categoryPath = uri.AbsolutePath
                            .Replace("/ru/animals/", "")
                            .Replace("/lv/animals/", "")
                            .Replace("/ru/agriculture/animal-husbandry/agricultural-animals/", "")
                            .Trim('/')
                            .Replace("/sell", "")
                            .ToLowerInvariant();

                        result.Add(new Pets
                        {
                            id = Guid.NewGuid(),
                            name = cleanTitle,
                            description = description,
                            age = AgeResolver.ParseAge(ageText),
                            breed_id = breedId,
                            species_id = speciesId.Value,
                            color = colorText,
                            gender_id = GenderResolver.ResolveGender(description, title),
                            mongo_image_id = photoId?.ToString(),
                            shelter_id = shelterId,
                            created_at = DateTime.UtcNow,
                            external_url = fullLink,
                            cena = priceText,
                            category = categoryPath
                        });

                        collectedFromCategory++;
                        Console.WriteLine($"✅ Added: {cleanTitle}");

                        if (collectedFromCategory >= max) break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ Error while parsing ad: " + ex.Message);
                        continue;
                    }
                }

                page++;
            }
        }
        Console.WriteLine($"📊 Total pets parsed: {result.Count}");

        return result;
    }

    private string? GetFieldValue(AngleSharp.Dom.IDocument doc, string fieldName)
    {
        return doc.QuerySelectorAll("tr")
            .FirstOrDefault(tr =>
                tr.Children.Length == 2 &&
                tr.Children[0].TextContent.Trim().StartsWith(fieldName, StringComparison.OrdinalIgnoreCase))
            ?.Children[1]
            ?.TextContent
            ?.Trim();
    }
}
