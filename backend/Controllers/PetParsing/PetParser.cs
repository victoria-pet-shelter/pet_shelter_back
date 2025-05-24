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
    private string? ExtractPrice(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        try
        {
            var match = Regex.Match(description, @"(\d[\d\s]*[\.,]?\d*)\s*€");
            if (match.Success)
            {
                string raw = match.Groups[1].Value;
                raw = raw.Replace(" ", "").Replace(",", ".");
                return raw;
            }
            else
            {
                Console.WriteLine("[DEBUG] Сena not in description.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error with cena: {ex.Message}");
        }

        return null;
    }

    public async Task<List<Pets>> ParseFromSsLvAsync(Guid shelterId, int max = 1000)
    {
        List<Pets> result = new();
        int page = 1;
        var client = _httpClientFactory.CreateClient();
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);

        while (result.Count < max)
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
                    if (!link.Contains("/msg/lv/animals/dogs/"))
                    {
                        Console.WriteLine($"⚠️ Skipped non-pet ad: {link}");
                        continue;
                    }

                    var fullLink = "https://www.ss.lv" + link;

                    if (await _db.Pets.AnyAsync(p => p.external_url == fullLink))
                    {
                        Console.WriteLine($"⚠️ Skipping duplicate: {fullLink}");
                        continue;
                    }

                    var petPageHtml = await client.GetStringAsync(fullLink);
                    var petDoc = await context.OpenAsync(req => req.Content(petPageHtml));

                    var breedText = GetFieldValue(petDoc, "Šķirne:");
                    Console.WriteLine($"[DEBUG] breedText: {breedText}");

                    var ageText = GetFieldValue(petDoc, "Vecums:");
                    Console.WriteLine($"[DEBUG] ageText: {ageText}");

                    var fullDescription = petDoc.QuerySelector("div[id^='msg_div_msg']")?.TextContent?.Trim();
                    Console.WriteLine($"[DEBUG] fullDescription: {fullDescription}");

                    var colorText = GetFieldValue(petDoc, "Krāsa:");
                    Console.WriteLine($"[DEBUG] colorText: {colorText}");

                    var healthText = GetFieldValue(petDoc, "Veselība:");
                    Console.WriteLine($"[DEBUG] healthText: {healthText}");

                    var priceText = ExtractPrice(fullDescription);
                    Console.WriteLine($"[DEBUG] cena: {priceText}");

                    string? imgElement = petDoc.QuerySelector("img[src*='/images/']")?.GetAttribute("src");
                    if (string.IsNullOrEmpty(imgElement))
                    {
                        var imageMeta = petDoc.QuerySelector("meta[property='og:image']")?.GetAttribute("content");
                        if (!string.IsNullOrEmpty(imageMeta))
                        {
                            imgElement = imageMeta;
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
                        external_url = fullLink,
                        cena = priceText

                    });

                    Console.WriteLine($"✅ Added pet: {shortTitle}");

                    if (result.Count >= max) break;
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

    private float ParseAge(string? ageText)
    {
        if (string.IsNullOrWhiteSpace(ageText))
        {
            Console.WriteLine("⚠️ ageText is empty or null.");
            return 0;
        }

        ageText = ageText.ToLower().Trim();
        Console.WriteLine($"[DEBUG] Parsing ageText: {ageText}");

        try
        {
            var match = Regex.Match(
                ageText,
                @"(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>gadi|gads|gadus|мес(?:яц[аев]*)?|месяц(?:а|ев)?|год(?:а|ов)?|лет|года|months?|years?|mēneš[iu]?|men|yr|yrs|y|m)",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
            {
                Console.WriteLine($"⚠️ Could not parse ageText: '{ageText}'");
                return 0;
            }

            var numberPart = match.Groups["value"].Value.Replace(',', '.');
            var unit = match.Groups["unit"].Value;

            float number = float.Parse(numberPart, System.Globalization.CultureInfo.InvariantCulture);

            if (unit.StartsWith("mēn") || unit.StartsWith("мес") || unit.StartsWith("men") || unit.StartsWith("m") || unit.StartsWith("month"))
            {
                return number;
            }
            else if (unit.StartsWith("gad") || unit.StartsWith("год") || unit.StartsWith("лет") || unit.StartsWith("yr") || unit.StartsWith("y") || unit.StartsWith("year"))
            {
                return number * 12;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to parse age: '{ageText}' | {ex.Message}");
        }

        return 0;
    }

    private async Task<int> ResolveBreedIdAsync(string? breedText)
    {
        if (string.IsNullOrEmpty(breedText))
        {
            Console.WriteLine("⚠️ breedText is empty. Defaulting to ID 1.");
            return 1;
        }

        var lower = breedText.ToLower().Trim();
        var breed = await _db.Breeds.FirstOrDefaultAsync(b => b.name.ToLower() == lower);

        if (breed == null)
        {
            Console.WriteLine($"⚠️ Breed '{breedText}' not found in database. Using default ID 1.");
        }

        return breed?.id ?? 1;
    }

    private int ResolveGender(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("⚠️ No gender-related text found.");
            return 1;
        }

        var lower = text.ToLower();
        Console.WriteLine($"[DEBUG] Checking gender text: {lower}");

        if (Regex.IsMatch(lower, @"\b(девочка|meitene|female|сука|girl|женский|she)\b"))
            return 2;

        if (Regex.IsMatch(lower, @"\b(мальчик|puika|male|кобель|boy|мужской|he)\b"))
            return 1;

        Console.WriteLine("⚠️ Gender could not be resolved. Defaulting to male.");
        return 1;
    }
}
