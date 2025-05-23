using System.Threading.Tasks;
using MongoDB.Driver.GridFS;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;
using System;

public class MongoService
{
    private readonly IMongoDatabase _database;
    private readonly GridFSBucket _bucket;

    public MongoService()
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost:27017";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("PetShelterMedia");
        _bucket = new GridFSBucket(_database);
    }

    public async Task<ObjectId> SaveImageAsync(byte[] data)
    {
        using var stream = new MemoryStream(data);
        var id = await _bucket.UploadFromStreamAsync(Guid.NewGuid().ToString(), stream);
        return id;
    }

    public async Task<byte[]> GetImageAsync(string id)
    {
        var objectId = ObjectId.Parse(id);
        using var stream = new MemoryStream();
        await _bucket.DownloadToStreamAsync(objectId, stream);
        return stream.ToArray();
    }
}
