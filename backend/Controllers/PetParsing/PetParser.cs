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
using ImageFetchers;

public class PetParser
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MongoService _mongoService;
    private readonly AppDbContext _db;
    private readonly BreedResolver _breedResolver;
    private readonly ImageFetcher _imageFetcher;
    private readonly SpeciesDetector _speciesDetector;

    public PetParser(IHttpClientFactory httpClientFactory, MongoService mongoService, AppDbContext db, BreedResolver breedResolver, ImageFetcher imageFetcher, SpeciesDetector speciesDetector)
    {
        _httpClientFactory = httpClientFactory;
        _mongoService = mongoService;
        _db = db;
        _breedResolver = breedResolver;
        _imageFetcher = imageFetcher;
        _speciesDetector = speciesDetector;
    }

    public async Task<List<Pets>> ParseFromSsLvAsync(Guid shelterId, int max = 50)
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
                    var fullLink = "https://www.ss.lv" + link;

                    if (fullLink.Contains("/dogs/accessories") || fullLink.Contains("/dogs/services"))
                    {
                        // Console.WriteLine($"⚠️ Skipped non-animal category: {fullLink}");
                        continue;
                    }

                    if (await _db.Pets.AnyAsync(p => p.external_url == fullLink))
                    {
                        // Console.WriteLine($"⚠️ Skipping duplicate: {fullLink}");
                        continue;
                    }

                    var petPageHtml = await client.GetStringAsync(fullLink);
                    var petDoc = await context.OpenAsync(req => req.Content(petPageHtml));

                    var pageTitle = petDoc.QuerySelector("title")?.TextContent?.Trim();
                    var cleanTitle = pageTitle;

                    if (!string.IsNullOrWhiteSpace(pageTitle))
                    {
                        var split = pageTitle.Split("€. ");
                        if (split.Length > 1)
                        {
                            cleanTitle = split[1];
                        }
                        else
                        {
                            cleanTitle = pageTitle;
                        }

                        cleanTitle = Regex.Replace(cleanTitle, @"-+\s*Sludinājumi\s*$", "", RegexOptions.IgnoreCase).Trim();
                    }


                    var breedText = GetFieldValue(petDoc, "Šķirne:");
                    // Console.WriteLine($"[DEBUG] breedText: {breedText}");

                    var ageText = GetFieldValue(petDoc, "Vecums:");
                    // Console.WriteLine($"[DEBUG] ageText: {ageText}");

                    var fullDescription = petDoc.QuerySelector("div[id^='msg_div_msg']")?.TextContent?.Trim();
                    // Console.WriteLine($"[DEBUG] fullDescription: {fullDescription}");

                    var colorText = GetFieldValue(petDoc, "Krāsa:");
                    // Console.WriteLine($"[DEBUG] colorText: {colorText}");

                    var priceText = PriceResolver.ExtractPrice(fullDescription);
                    // Console.WriteLine($"[DEBUG] cena: {priceText}");

                    // Image Download
                    ObjectId? photoId = await _imageFetcher.FetchImageIdFromPage(petDoc);


                    var shortTitle = !string.IsNullOrWhiteSpace(pageTitle)
                        ? pageTitle
                        : (fullDescription != null && fullDescription.Length > 100
                            ? fullDescription.Substring(0, 100) + "..."
                            : fullDescription ?? "No name");

                    int breedId = await _breedResolver.ResolveBreedIdAsync(breedText);
                    var breed = await _db.Breeds.FindAsync(breedId);

                    if (breed == null)
                    {
                        Console.WriteLine($"⚠️ Failed to find or create breed: {breedText}");
                        continue;
                    }

                    int? speciesId = breed.species_id;

                    result.Add(new Pets
                    {
                        id = Guid.NewGuid(),
                        name = cleanTitle,
                        description = fullDescription,
                        age = AgeResolver.ParseAge(ageText),
                        breed_id = breedId,
                        species_id = speciesId ?? 1,
                        color = colorText,
                        gender_id = GenderResolver.ResolveGender(fullDescription, pageTitle),
                        mongo_image_id = photoId?.ToString(),
                        shelter_id = shelterId,
                        created_at = DateTime.UtcNow,
                        external_url = fullLink,
                        cena = priceText
                    });

                    Console.WriteLine($"✅ Added pet: {cleanTitle}");

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
}