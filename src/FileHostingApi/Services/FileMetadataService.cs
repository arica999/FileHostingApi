using FileHostingApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FileHostingApi.Services
{
    public class FileMetadataService
    {
        private readonly IMongoCollection<FileMetadata>? _fileMetadata;
        private static readonly List<FileMetadata> _inMemory = new List<FileMetadata>();
        private static readonly object _lock = new object();

        public FileMetadataService(IConfiguration config)
        {
            try
            {
                var conn = config["MongoDbSettings:ConnectionString"];
                var db = config["MongoDbSettings:Database"];
                if (!string.IsNullOrWhiteSpace(conn) && !string.IsNullOrWhiteSpace(db))
                {
                    var client = new MongoClient(conn);
                    var database = client.GetDatabase(db);
                    _fileMetadata = database.GetCollection<FileMetadata>("files");
                }
            }
            catch
            {
                _fileMetadata = null; // fall back to in-memory storage
            }
        }

        public async Task<List<FileMetadata>> GetAsync()
        {
            if (_fileMetadata != null)
            {
                return await _fileMetadata.Find(_ => true).ToListAsync();
            }

            lock (_lock)
            {
                return _inMemory.Select(f => new FileMetadata
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    UploadDate = f.UploadDate,
                    Size = f.Size,
                    Uploader = f.Uploader
                }).ToList();
            }
        }

        public async Task<FileMetadata?> GetByIdAsync(string id)
        {
            if (_fileMetadata != null)
            {
                return await _fileMetadata.Find(fm => fm.Id == id).FirstOrDefaultAsync();
            }

            lock (_lock)
            {
                return _inMemory.FirstOrDefault(f => f.Id == id);
            }
        }

        public async Task<FileMetadata> CreateAsync(FileMetadata fileMetadata)
        {
            if (string.IsNullOrWhiteSpace(fileMetadata.Id))
            {
                fileMetadata.Id = ObjectId.GenerateNewId().ToString();
            }

            if (_fileMetadata != null)
            {
                await _fileMetadata.InsertOneAsync(fileMetadata);
                return fileMetadata;
            }

            lock (_lock)
            {
                _inMemory.Add(fileMetadata);
            }
            return fileMetadata;
        }

        public async Task DeleteAsync(string id)
        {
            if (_fileMetadata != null)
            {
                await _fileMetadata.DeleteOneAsync(fm => fm.Id == id);
                return;
            }

            lock (_lock)
            {
                var idx = _inMemory.FindIndex(f => f.Id == id);
                if (idx >= 0)
                    _inMemory.RemoveAt(idx);
            }
        }
    }
}
