namespace FileHostingApi.Models
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; }
        public string Uploader { get; set; }
    }
}
