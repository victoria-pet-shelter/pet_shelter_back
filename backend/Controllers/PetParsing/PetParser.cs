using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using Models;
using MongoDB.Bson;

public class PetParser
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MongoService _mongoService;
    private readonly AppDbContext _db;

    public PetParser(IHttpClientFactory httpClientFactory, MongoService mongoService, AppDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _mongoService = mongoService;
        _db = db;
        EnsureGendersExist().Wait();
    }

    private async Task EnsureGendersExist()
    {
        var existing = await _db.Genders.ToListAsync();
        if (!existing.Any(g => g.id == 1))
            await _db.Genders.AddAsync(new Genders { id = 1, name = "male" });
        if (!existing.Any(g => g.id == 2))
            await _db.Genders.AddAsync(new Genders { id = 2, name = "female" });

        await _db.SaveChangesAsync();
    }

    public async Task<List<Pets>> ParseFromSsLvAsync(Guid shelterId)
    {
        List<Pets> result = new();
        var page = 1;
        var client = _httpClientFactory.CreateClient();
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);

        while (result.Count < 10)
        {
            var url = $"https://www.ss.lv/lv/animals/dogs/page{(page > 1 ? page.ToString() : "")}.html";
            var html = await client.GetStringAsync(url);
            var document = await context.OpenAsync(req => req.Content(html));
            var ads = document.QuerySelectorAll(".d1").Where(e => e.QuerySelector("a") != null);

            if (!ads.Any())
            {
                Console.WriteLine("⚠️ No more ads found.");
                break;
            }

            Console.WriteLine($"Found {ads.Count()} ads on page {page}");

            foreach (var ad in ads)
            {
                try
                {
                    var link = ad.QuerySelector("a")?.GetAttribute("href");
                    if (string.IsNullOrEmpty(link)) continue;

                    var fullLink = "https://www.ss.lv" + link;

                    bool alreadyExists = await _db.Pets.AnyAsync(p => p.external_url == fullLink);
                    if (alreadyExists)
                    {
                        Console.WriteLine($"⚠️ Skipping duplicate: {fullLink}");
                        continue;
                    }

                    var petPageHtml = await client.GetStringAsync(fullLink);
                    var petDoc = await context.OpenAsync(req => req.Content(petPageHtml));

                    var breedText = petDoc.QuerySelector("td:has(span:contains('Šķirne:')) + td")?.TextContent?.Trim();
                    var ageText = petDoc.QuerySelector("td:has(span:contains('Vecums:')) + td")?.TextContent?.Trim();
                    var fullDescription = petDoc.QuerySelector("div[id^='msg_div_msg']")?.TextContent?.Trim();
                    var colorText = petDoc.QuerySelector("td:has(span:contains('Krāsa:')) + td")?.TextContent?.Trim();
                    var healthText = petDoc.QuerySelector("td:has(span:contains('Veselība:')) + td")?.TextContent?.Trim();

                    // попытка вытащить URL изображения
                    string? imgElement = petDoc.QuerySelector("img[src*='/images/']")?.GetAttribute("src");

                    // если img нет, пробуем достать background-image из стиля
                    if (string.IsNullOrEmpty(imgElement))
                    {
                        var divWithBackground = petDoc.QuerySelector("div[style*='background-image']");
                        var style = divWithBackground?.GetAttribute("style");

                        if (!string.IsNullOrEmpty(style))
                        {
                            var match = Regex.Match(style, @"url\(['""]?(.*?)['""]?\)");
                            if (match.Success)
                            {
                                imgElement = match.Groups[1].Value;
                            }
                        }
                    }

                    ObjectId? photoId = null;
                    if (!string.IsNullOrEmpty(imgElement))
                    {
                        try
                        {
                            var imageUrl = imgElement.StartsWith("http") ? imgElement : "https:" + imgElement;
                            var imageBytes = await client.GetByteArrayAsync(imageUrl);
                            photoId = await _mongoService.SaveImageAsync(imageBytes);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Failed to fetch image: {imgElement} | {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No image found on the page.");
                    }

                    var shortTitle = fullDescription != null && fullDescription.Length > 100
                        ? fullDescription.Substring(0, 100) + "..."
                        : fullDescription ?? "No name";

                    result.Add(new Pets
                    {
                        id = Guid.NewGuid(),
                        name = shortTitle,
                        description = fullDescription,
                        age = ParseAge(ageText),
                        breed_id = await ResolveBreedIdAsync(breedText),
                        color = colorText,
                        health = healthText,
                        species_id = 1,
                        gender_id = ResolveGender(fullDescription),
                        mongo_image_id = photoId?.ToString(),
                        shelter_id = shelterId,
                        created_at = DateTime.UtcNow,
                        external_url = fullLink
                    });

                    Console.WriteLine($"✅ Added pet: {shortTitle}");

                    if (result.Count >= 10) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Parse error: " + ex.Message);
                    continue;
                }
            }
            
            page++;
        }

        return result;
    }

    private float ParseAge(string? ageText)
    {
        if (string.IsNullOrWhiteSpace(ageText))
            return 0;

        var parts = ageText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            if (parts[1].StartsWith("mēn")) return float.Parse(parts[0]);
            if (parts[1].StartsWith("gad")) return float.Parse(parts[0]) * 12;
        }

        return 0;
    }

    private async Task<int> ResolveBreedIdAsync(string? breedText)
    {
        if (string.IsNullOrEmpty(breedText)) return 1;

        var breed = await _db.Breeds.FirstOrDefaultAsync(b => b.name.ToLower() == breedText.ToLower());
        if (breed != null) return breed.id;

        return 1;
    }

    private int ResolveGender(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        var lower = text.ToLower();

        if (Regex.IsMatch(lower, @"\b(девочка|meitene|female|сука)\b")) return 2;
        if (Regex.IsMatch(lower, @"\b(мальчик|puika|male|кобель)\b")) return 1;

        return 1;
    }
}
