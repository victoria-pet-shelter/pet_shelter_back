using System.Threading.Tasks;
using MongoDB.Driver.GridFS; // This is a special file storage system in MongoDB
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;
using System;

public class MongoService
{
    private readonly IMongoDatabase _database;
    private readonly GridFSBucket _bucket;

    // Initializes MongoDB connection and sets up GridFS bucket
    public MongoService()
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost:27017";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("PetShelterMedia");
        _bucket = new GridFSBucket(_database);
    }
    public GridFSBucket GetBucket()
    {
        return new GridFSBucket(_database);
    }

    public async Task<ObjectId> SaveImageAsync(byte[] data)
    {
        using var stream = new MemoryStream(data); // Wrap byte array in stream
        var id = await _bucket.UploadFromStreamAsync(Guid.NewGuid().ToString(), stream); // Store file with random name
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
