using FileHostingApi.Models;
using FileHostingApi.Services;

namespace FileHostingApi.Tests
{
    public class MockFileMetadataService : IFileMetadataService
    {
        private readonly Dictionary<string, FileMetadata> _metadata = new();

        public Task<List<FileMetadata>> GetAsync()
        {
            return Task.FromResult(new List<FileMetadata>(_metadata.Values));
        }

        public Task<FileMetadata?> GetByIdAsync(string id)
        {
            _metadata.TryGetValue(id, out var meta);
            return Task.FromResult(meta);
        }

        public Task<FileMetadata> CreateAsync(FileMetadata newFile)
        {
            _metadata[newFile.Id] = newFile;
            return Task.FromResult(newFile);
        }

        public Task DeleteAsync(string id)
        {
            _metadata.Remove(id);
            return Task.CompletedTask;
        }
    }
}