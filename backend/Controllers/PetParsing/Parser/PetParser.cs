using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using ImageFetchers;
using MongoDB.Bson;
using System.Linq;
using AngleSharp;
using Models;
using System;

public class PetParser
{
    // Dependencies and services
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MongoService _mongoService;
    private readonly AppDbContext _db;
    private readonly BreedResolver _breedResolver;
    private readonly GenderResolver _genderResolver;
    private readonly ImageFetcher _imageFetcher;
    private readonly string _linkPath;

    // Constructor injecting required services
    public PetParser(
        IHttpClientFactory httpClientFactory,
        MongoService mongoService,
        AppDbContext db,
        BreedResolver breedResolver,
        GenderResolver genderResolver,
        ImageFetcher imageFetcher)
    {
        _httpClientFactory = httpClientFactory;
        _mongoService = mongoService;
        _db = db;
        _breedResolver = breedResolver;
        _genderResolver = genderResolver;
        _imageFetcher = imageFetcher;
        _linkPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "SsLvLinks.json");
    }


    private static readonly Dictionary<string, int> _categorySpeciesMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dogs"] = 1,
        ["cats"] = 2,
        ["exotic-animals"] = 3,
        ["rodents/degu"] = 4,
        ["rodents/domestic-rats"] = 4,
        ["rodents/rabbits"] = 4,
        ["rodents/guinea-pigs"] = 4,
        ["rodents/ferret"] = 4,
        ["rodents/hamsters"] = 4,
        ["rodents/chinchillas"] = 4,
        ["parrots-and-birds/canaries"] = 5,
        ["parrots-and-birds/parrots"] = 5,
        ["fish/fish"] = 6,
        ["agricultural-animals/rams-sheeps"] = 7,
        ["agricultural-animals/goats"] = 7,
        ["agricultural-animals/large-horned-livestock"] = 7,
        ["agricultural-animals/pigs"] = 7,
        ["agricultural-animals/horses-donkeys-other"] = 7,
        ["agricultural-animals/rabbits-nutrias"] = 7
    };


    // Main parsing function
    public async Task<List<Pets>> ParseFromSsLvAsync(Guid shelterId, int max = 50)
    {
        var result = new List<Pets>();
        var client = _httpClientFactory.CreateClient();
        var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader()); // Setup AngleSharp browsing context

        var stats = new Dictionary<string, (int added, int skipped)>(); // Track stats per category
        string logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Logs", $"parse_{DateTime.Now:yyyyMMdd_HHmmss}.log"));
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        using var logWriter = new StreamWriter(logPath); // Log writer

        List<string> urls = await LoadUrlsAsync(); // Load category URLs from JSON file

        if (urls.Count == 0)
            return result; // No URLs = no parsing

        foreach (var baseUrl in urls) // Loop through each category URL
        {
            int page = 1;
            int collected = 0; // Count collected items
            int added = 0;
            int skipped = 0;

            // Extract category name from URL path
            // Load first page for comparison
            string firstPageUrl = baseUrl + "index.html";
            var firstPageDoc = await TryLoadPageAsync(client, context, firstPageUrl);
            string? firstPageHtml = firstPageDoc?.DocumentElement?.OuterHtml;

            while (true) // Loop through paginated pages
            {
                string url = baseUrl + (page > 1 ? $"page{page}.html" : "index.html");
                Console.WriteLine($"🔗 Parsing: {url}");
                await logWriter.WriteLineAsync($"🔗 Parsing: {url}");

                var document = await TryLoadPageAsync(client, context, url);
                if (document == null)
                {
                    Console.WriteLine("⚠️ Failed to load page — stopping category.");
                    await logWriter.WriteLineAsync("⚠️ Failed to load page — stopping category.");
                    break;
                }

                string? currentPageHtml = document.DocumentElement?.OuterHtml;

                // Stop if duplicate page
                if (!string.IsNullOrEmpty(firstPageHtml) &&
                    !string.IsNullOrEmpty(currentPageHtml) &&
                    page > 1 &&
                    currentPageHtml == firstPageHtml)
                {
                    Console.WriteLine("🔁 Page identical to first — stopping category.");
                    await logWriter.WriteLineAsync("🔁 Page identical to first — stopping category.");
                    break;
                }

                // Select ad blocks
                var ads = document.QuerySelectorAll(".d1")
                    .Where(e => e.QuerySelector("a")?.GetAttribute("href")?.Contains("/msg/") == true)
                    .ToList();

                if (ads.Count == 0)
                {
                    Console.WriteLine("🚫 No ads found — stopping this category.");
                    await logWriter.WriteLineAsync("🚫 No ads found — stopping this category.");
                    break;
                }

                // Process each ad
                foreach (var ad in ads)
                {
                    string? fullLink = GetAdLink(ad); // Get full URL to ad
                    if (string.IsNullOrWhiteSpace(fullLink) ||
                        await _db.Pets.AnyAsync(p => p.external_url == fullLink) ||
                        result.Any(p => p.external_url == fullLink)) // Skip duplicates
                    {
                        skipped++;
                        continue;
                    }

                    var pet = await ParseAdAsync(fullLink, context, client, shelterId, baseUrl); // Parse single ad
                    if (pet != null)
                    {
                        result.Add(pet);
                        collected++;
                        added++;
                        Console.WriteLine($"✅ Added: {pet.name}");
                        await logWriter.WriteLineAsync($"✅ Added: {pet.name}");
                    }
                    else
                    {
                        skipped++;
                    }

                    if (collected >= max) // Stop if reached limit
                    {
                        Console.WriteLine("📦 Reached max — moving to next category.");
                        await logWriter.WriteLineAsync("📦 Reached max — moving to next category.");
                        break;
                    }
                }

                if (collected >= max)
                    break;

                page++;
            }

            stats[ExtractCategoryPath(baseUrl)] = (added, skipped); // Save stats for category
            await logWriter.WriteLineAsync($"📁 {ExtractCategoryPath(baseUrl)}: ✅ {added}, ❌ {skipped}");
        }

        // Log summary
        await logWriter.WriteLineAsync("\n📊 Summary:");
        foreach (var kvp in stats)
            await logWriter.WriteLineAsync($"📁 {kvp.Key}: ✅ {kvp.Value.added} / ❌ {kvp.Value.skipped}");

        await logWriter.FlushAsync();
        Console.WriteLine($"📂 Log saved: {logPath}");

        return result; // Return parsed pets
    }

    // Loads URLs from SsLvLinks.json file
    private async Task<List<string>> LoadUrlsAsync()
    {
        try
        {
            var json = await File.ReadAllTextAsync(_linkPath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to read SsLvLinks.json: {ex.Message}");
            return new();
        }
    }

    // Loads and parses a page given a URL
    private async Task<AngleSharp.Dom.IDocument?> TryLoadPageAsync(HttpClient client, IBrowsingContext context, string url)
    {
        try
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var html = await response.Content.ReadAsStringAsync();
            return await context.OpenAsync(req => req.Content(html));
        }
        catch
        {
            Console.WriteLine($"⚠️ Failed to fetch: {url}");
            return null;
        }
    }

    // Extracts ad link from ad block
    private string? GetAdLink(AngleSharp.Dom.IElement ad)
    {
        var link = ad.QuerySelector("a")?.GetAttribute("href");
        return string.IsNullOrWhiteSpace(link) ? null : "https://www.ss.lv" + link;
    }

    // Parses single ad page into a Pets object
    private async Task<Pets?> ParseAdAsync(string fullLink, IBrowsingContext context, HttpClient client, Guid shelterId, string baseUrl)
    {
        try
        {
            var petHtml = await client.GetStringAsync(fullLink);
            var petDoc = await context.OpenAsync(req => req.Content(petHtml));

            var title = petDoc.QuerySelector("title")?.TextContent?.Trim();
            var cleanTitle = Regex.Replace(title?.Split("€. ").LastOrDefault() ?? title ?? "Unnamed", "-+\\s*Sludinājumi\\s*$", "", RegexOptions.IgnoreCase).Trim();

            var breedText = GetFieldValue(petDoc, "Šķirne:") ?? GetFieldValue(petDoc, "Порода:");
            var ageText = GetFieldValue(petDoc, "Vecums:") ?? GetFieldValue(petDoc, "Возраст:");
            var colorText = GetFieldValue(petDoc, "Krāsa:") ?? GetFieldValue(petDoc, "Окрас:");
            var description = petDoc.QuerySelector("div[id^='msg_div_msg']")?.TextContent?.Trim();
            var priceText = PriceResolver.ExtractPrice(description);
            var photoId = await _imageFetcher.FetchImageIdFromPage(petDoc);


            string categoryPath = ExtractCategoryPath(baseUrl);
            int speciesId = _categorySpeciesMap.TryGetValue(categoryPath, out var id) ? id : 9;

            int breedId = await _breedResolver.ResolveBreedIdAsync(breedText, speciesId);
            int? genderId = await _genderResolver.ResolveGenderAsync(description, title);

            return new Pets
            {
                id = Guid.NewGuid(),
                name = cleanTitle,
                description = description,
                age = AgeResolver.ParseAge(ageText),
                breed_id = breedId,
                species_id = speciesId,
                color = colorText,
                gender_id = genderId,
                mongo_image_id = photoId?.ToString(),
                shelter_id = shelterId,
                created_at = DateTime.UtcNow,
                external_url = fullLink,
                cena = priceText,
                category = categoryPath
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error parsing ad: " + ex.Message);
            return null;
        }
    }

    // Extracts text content for a label in a table row
    private string? GetFieldValue(AngleSharp.Dom.IDocument doc, string fieldName)
    {
        return doc.QuerySelectorAll("tr")
            .FirstOrDefault(tr =>
                tr.Children.Length == 2 &&
                tr.Children[0].TextContent.Trim().StartsWith(fieldName, StringComparison.OrdinalIgnoreCase))
            ?.Children[1]?.TextContent?.Trim();
    }

    private static string ExtractCategoryPath(string url)
    {
        Uri uri = new Uri(url);
        return uri.AbsolutePath
            .Replace("/ru/animals/", "")
            .Replace("/lv/animals/", "")
            .Replace("/ru/agriculture/animal-husbandry/agricultural-animals/", "")
            .Trim('/')
            .Replace("/sell", "")
            .ToLowerInvariant();
    }
}