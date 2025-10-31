using FileHostingApi.Models;
using FileHostingApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileHostingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly FileMetadataService _fileMetadataService;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

        public FileController(FileMetadataService fileMetadataService)
        {
            _fileMetadataService = fileMetadataService;
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadFileRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File not selected.");

            var filePath = Path.Combine(_storagePath, request.File.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            var metadata = new FileMetadata
            {
                FileName = request.File.FileName,
                UploadDate = DateTime.UtcNow,
                Size = request.File.Length,
                Uploader = request.Uploader ?? "anonymous"
            };
            await _fileMetadataService.CreateAsync(metadata);

            return Ok(new { message = "File uploaded successfully.", metadata });
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadById(string id)
        {
            var metadata = await _fileMetadataService.GetByIdAsync(id);
            if (metadata == null)
                return NotFound("File not found by ID.");

            var filePath = Path.Combine(_storagePath, metadata.FileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("Physical file not present on server.");

            var mimeType = "application/octet-stream";
            return PhysicalFile(filePath, mimeType, metadata.FileName);
        }

        [HttpGet("download/byname/{filename}")]
        public IActionResult DownloadByName(string filename)
        {
            var filePath = Path.Combine(_storagePath, filename);
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found by name on server.");

            var mimeType = "application/octet-stream";
            return PhysicalFile(filePath, mimeType, filename);
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var files = await _fileMetadataService.GetAsync();
            return Ok(files);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(string id)
        {
            var metadata = await _fileMetadataService.GetByIdAsync(id);
            if (metadata == null)
                return NotFound("Metadata not found for file with given ID.");

            var filePath = Path.Combine(_storagePath, metadata.FileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            await _fileMetadataService.DeleteAsync(id);
            return Ok("File and metadata deleted.");
        }
    }
}
