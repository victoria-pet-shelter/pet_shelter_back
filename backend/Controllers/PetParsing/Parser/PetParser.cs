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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MongoService _mongoService;
    private readonly AppDbContext _db;
    private readonly BreedResolver _breedResolver;
    private readonly GenderResolver _genderResolver;
    private readonly ImageFetcher _imageFetcher;
    private readonly SpeciesDetector _speciesDetector;
    private readonly string _linkPath;

    public PetParser(
        IHttpClientFactory httpClientFactory,
        MongoService mongoService,
        AppDbContext db,
        BreedResolver breedResolver,
        GenderResolver genderResolver,
        ImageFetcher imageFetcher,
        SpeciesDetector speciesDetector)
    {
        _httpClientFactory = httpClientFactory;
        _mongoService = mongoService;
        _db = db;
        _breedResolver = breedResolver;
        _genderResolver = genderResolver;
        _imageFetcher = imageFetcher;
        _speciesDetector = speciesDetector;
        _linkPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "SsLvLinks.json");
    }

    public async Task<List<Pets>> ParseFromSsLvAsync(Guid shelterId, int max = 1)
    {
        var result = new List<Pets>();
        var client = _httpClientFactory.CreateClient();
        var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

        var stats = new Dictionary<string, (int added, int skipped)>();
        string logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Logs", $"parse_{DateTime.Now:yyyyMMdd_HHmmss}.log"));
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        using var logWriter = new StreamWriter(logPath);

        List<string> urls = await LoadUrlsAsync();
        if (urls.Count == 0)
            return result;

        foreach (var baseUrl in urls)
        {
            int page = 1;
            int collected = 0;
            int added = 0;
            int skipped = 0;

            Uri uri = new Uri(baseUrl);
            string categoryPath = uri.AbsolutePath
                .Replace("/ru/animals/", "")
                .Replace("/lv/animals/", "")
                .Replace("/ru/agriculture/animal-husbandry/agricultural-animals/", "")
                .Trim('/')
                .Replace("/sell", "")
                .ToLowerInvariant();

            string firstPageUrl = baseUrl + "index.html";
            var firstPageDoc = await TryLoadPageAsync(client, context, firstPageUrl);
            string? firstPageHtml = firstPageDoc?.DocumentElement?.OuterHtml;

            while (true)
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

                if (!string.IsNullOrEmpty(firstPageHtml) &&
                    !string.IsNullOrEmpty(currentPageHtml) &&
                    page > 1 &&
                    currentPageHtml == firstPageHtml)
                {
                    Console.WriteLine("🔁 Page identical to first — stopping category.");
                    await logWriter.WriteLineAsync("🔁 Page identical to first — stopping category.");
                    break;
                }

                var ads = document.QuerySelectorAll(".d1")
                    .Where(e => e.QuerySelector("a")?.GetAttribute("href")?.Contains("/msg/") == true)
                    .ToList();

                if (ads.Count == 0)
                {
                    Console.WriteLine("🚫 No ads found — stopping this category.");
                    await logWriter.WriteLineAsync("🚫 No ads found — stopping this category.");
                    break;
                }

                foreach (var ad in ads)
                {
                    string? fullLink = GetAdLink(ad);
                    if (string.IsNullOrWhiteSpace(fullLink) ||
                        await _db.Pets.AnyAsync(p => p.external_url == fullLink) ||
                        result.Any(p => p.external_url == fullLink))
                    {
                        skipped++;
                        continue;
                    }

                    var pet = await ParseAdAsync(fullLink, context, client, shelterId, baseUrl);
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

                    if (collected >= max)
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

            stats[categoryPath] = (added, skipped);
            await logWriter.WriteLineAsync($"📁 {categoryPath}: ✅ {added}, ❌ {skipped}");
        }

        await logWriter.WriteLineAsync("\n📊 Summary:");
        foreach (var kvp in stats)
            await logWriter.WriteLineAsync($"📁 {kvp.Key}: ✅ {kvp.Value.added} / ❌ {kvp.Value.skipped}");

        await logWriter.FlushAsync();
        Console.WriteLine($"📂 Log saved: {logPath}");

        return result;
    }

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

    private string? GetAdLink(AngleSharp.Dom.IElement ad)
    {
        var link = ad.QuerySelector("a")?.GetAttribute("href");
        return string.IsNullOrWhiteSpace(link) ? null : "https://www.ss.lv" + link;
    }

    private async Task<Pets?> ParseAdAsync(string fullLink, IBrowsingContext context, HttpClient client, Guid shelterId, string baseUrl)
    {
        try
        {
            var petHtml = await client.GetStringAsync(fullLink);
            var petDoc = await context.OpenAsync(req => req.Content(petHtml));

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
            int? genderId = await _genderResolver.ResolveGenderAsync(description, title);

            Uri uri = new Uri(baseUrl);
            string categoryPath = uri.AbsolutePath
                .Replace("/ru/animals/", "")
                .Replace("/lv/animals/", "")
                .Replace("/ru/agriculture/animal-husbandry/agricultural-animals/", "")
                .Trim('/')
                .Replace("/sell", "")
                .ToLowerInvariant();

            return new Pets
            {
                id = Guid.NewGuid(),
                name = cleanTitle,
                description = description,
                age = AgeResolver.ParseAge(ageText),
                breed_id = breedId,
                species_id = speciesId.Value,
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

    private string? GetFieldValue(AngleSharp.Dom.IDocument doc, string fieldName)
    {
        return doc.QuerySelectorAll("tr")
            .FirstOrDefault(tr =>
                tr.Children.Length == 2 &&
                tr.Children[0].TextContent.Trim().StartsWith(fieldName, StringComparison.OrdinalIgnoreCase))
            ?.Children[1]?.TextContent?.Trim();
    }
}
