using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using MongoDB.Bson;
using AngleSharp;
using Models;
using System;


public class PetParser
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MongoService _mongoService;

    public PetParser(IHttpClientFactory httpClientFactory, MongoService mongoService)
    {
        _httpClientFactory = httpClientFactory;
        _mongoService = mongoService;
    }

    public async Task<List<Pets>> ParseFromSsLvAsync(Guid shelterId)
    {
        List<Pets> result = new();

        var url = "https://www.ss.lv/lv/animals/dogs";
        var client = _httpClientFactory.CreateClient();
        var html = await client.GetStringAsync(url);

        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html));

        var ads = document.QuerySelectorAll(".d1")
            .Where(e => e.QuerySelector("a") != null);

        Console.WriteLine($"Found {ads.Count()} ads");
        foreach (var ad in ads)
        {
            try
            {
                var link = ad.QuerySelector("a")?.GetAttribute("href");
                var fullLink = "https://www.ss.lv" + link;

                var fullText = ad.TextContent?.Trim().Replace("\n", " ").Replace("  ", " ") ?? "Без названия";
                var shortTitle = fullText.Length > 100 ? fullText[..100] + "..." : fullText;


                var img = ad.QuerySelector("img")?.GetAttribute("src");
                var imgUrl = string.IsNullOrEmpty(img) ? null : "https://i.ss.lv" + img;

                ObjectId? photoId = null;
                if (imgUrl != null)
                {
                    var imageBytes = await client.GetByteArrayAsync(imgUrl);
                    photoId = await _mongoService.SaveImageAsync(imageBytes);
                }

                result.Add(new Pets
                {
                    id = Guid.NewGuid(),
                    name = title,
                    description = "Imported from ss.lv",
                    shelter_id = shelterId,
                    created_at = DateTime.UtcNow,
                    species_id = 1, // in the time for test
                    breed_id = 1,   // in the time for test
                    gender_id = 1,
                    external_url = fullLink,
                    photo_id = photoId?.ToString()
                });

                if (result.Count >= 100) break; // >= 100 pets
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Parse error: " + ex.Message);
                continue;
            }
        }

        return result;
    }
}
