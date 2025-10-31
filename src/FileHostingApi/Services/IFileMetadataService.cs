using FileHostingApi.Models;

namespace FileHostingApi.Services
{
    public interface IFileMetadataService
    {
        Task<List<FileMetadata>> GetAsync();
        Task<FileMetadata?> GetByIdAsync(string id);
        Task<FileMetadata> CreateAsync(FileMetadata fileMetadata);
        Task DeleteAsync(string id);
    }
}