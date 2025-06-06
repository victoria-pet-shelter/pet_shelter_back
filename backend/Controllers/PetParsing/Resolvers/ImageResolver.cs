using System.Threading.Tasks;
using System.Net.Http;
using AngleSharp.Dom;
using MongoDB.Bson;
using System;

namespace ImageFetchers
{
    public class ImageFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly MongoService _mongoService;

        public ImageFetcher(HttpClient httpClient, MongoService mongoService)
        {
            _httpClient = httpClient;
            _mongoService = mongoService;
        }

        public async Task<ObjectId?> FetchImageIdFromPage(IDocument petDoc)
        {
            string? imgElement = null;

            imgElement =
                petDoc.QuerySelector("img[src*='/img/classified/']")?.GetAttribute("src") ??
                petDoc.QuerySelector("img[src*='/storage/']")?.GetAttribute("src") ??
                petDoc.QuerySelector("meta[property='og:image']")?.GetAttribute("content") ??
                petDoc.QuerySelector("div.pic_dv_thumbnail img")?.GetAttribute("src");

            if (string.IsNullOrEmpty(imgElement))
            {
                Console.WriteLine("⚠️ No image found on the page.");
                return null;
            }

            if (imgElement.EndsWith(".t.jpg"))
            {
                imgElement = imgElement.Replace(".t.jpg", ".800.jpg");
            }


            try
            {
                var imageUrl = imgElement.StartsWith("http") ? imgElement : "https:" + imgElement;
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                return await _mongoService.SaveImageAsync(imageBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to fetch image: {imgElement} | {ex.Message}");
                return null;
            }
        }
    }
}