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
    private readonly BreedResolver _breedResolver;
    private readonly WikidataFetcher _fetcher;

    public PetParser(IHttpClientFactory httpClientFactory, MongoService mongoService, AppDbContext db, BreedResolver breedResolver, WikidataFetcher fetcher)
    {
        _httpClientFactory = httpClientFactory;
        _mongoService = mongoService;
        _db = db;
        _breedResolver = breedResolver;
        _fetcher = fetcher;
    }

    public async Task<List<Pets>> ParseFromSsLvAsync(Guid shelterId, int max = 1)
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
                    // Console.WriteLine($"[DEBUG] ageText: {ageText}");

                    var fullDescription = petDoc.QuerySelector("div[id^='msg_div_msg']")?.TextContent?.Trim();
                    Console.WriteLine($"[DEBUG] fullDescription: {fullDescription}");

                    var colorText = GetFieldValue(petDoc, "Krāsa:");
                    // Console.WriteLine($"[DEBUG] colorText: {colorText}");

                    var healthText = GetFieldValue(petDoc, "Veselība:");
                    // Console.WriteLine($"[DEBUG] healthText: {healthText}");

                    var priceText = PriceResolver.ExtractPrice(fullDescription);
                    // Console.WriteLine($"[DEBUG] cena: {priceText}");

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

                    int speciesId = _fetcher.InferSpeciesId(breedText ?? "");
                    if (speciesId == 0)
                    {
                        Console.WriteLine($"⚠️ Skipping unknown species for breed: {breedText}");
                        continue;
                    }

                    result.Add(new Pets
                    {
                        id = Guid.NewGuid(),
                        name = shortTitle,
                        description = fullDescription,
                        age = AgeResolver.ParseAge(ageText),
                        breed_id = await _breedResolver.ResolveBreedIdAsync(breedText),
                        species_id = speciesId,
                        color = colorText,
                        health = healthText,
                        gender_id = GenderResolver.ResolveGender(fullDescription),
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
}