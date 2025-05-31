using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Threading.Tasks;

namespace Controllers
{
    [ApiController]
    [Route("image")]
    public class ImageController : ControllerBase
    {
        private readonly GridFSBucket _bucket;

        public ImageController(IMongoDatabase mongoDatabase)
        {
            _bucket = new GridFSBucket(mongoDatabase);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetImageById(string id)
        {
            ObjectId objectId;

            if (!ObjectId.TryParse(id, out objectId))
            {
                if (Guid.TryParse(id, out Guid guid))
                {
                    try
                    {
                        var bytes = guid.ToByteArray();
                        objectId = new ObjectId(bytes[..12]);
                    }
                    catch
                    {
                        return BadRequest("❌ Failed to convert Guid to ObjectId.");
                    }
                }
                else
                {
                    return BadRequest("❌ Invalid image ID format.");
                }
            }

            try
            {
                var stream = await _bucket.OpenDownloadStreamAsync(objectId);
                return File(stream, "image/jpeg");
            }
            catch (GridFSFileNotFoundException)
            {
                return NotFound("❌ Image not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error while fetching image: " + ex.Message);
                return StatusCode(500, "❌ Internal server error.");
            }
        }
    }
}
