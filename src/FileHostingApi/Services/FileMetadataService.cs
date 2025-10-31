using FileHostingApi.Models;
using MongoDB.Driver;

namespace FileHostingApi.Services
{
    public class FileMetadataService
    {
        private readonly IMongoCollection<FileMetadata> _fileMetadata;

        public FileMetadataService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDbSettings:ConnectionString"]);
            var database = client.GetDatabase(config["MongoDbSettings:Database"]);
            _fileMetadata = database.GetCollection<FileMetadata>("files");
        }

        public async Task<List<FileMetadata>> GetAsync() =>
            await _fileMetadata.Find(_ => true).ToListAsync();

        public async Task<FileMetadata> GetByIdAsync(string id) =>
            await _fileMetadata.Find(fm => fm.Id == id).FirstOrDefaultAsync();

        public async Task<FileMetadata> CreateAsync(FileMetadata fileMetadata)
        {
            await _fileMetadata.InsertOneAsync(fileMetadata);
            return fileMetadata;
        }

        public async Task DeleteAsync(string id) =>
            await _fileMetadata.DeleteOneAsync(fm => fm.Id == id);
    }
}
