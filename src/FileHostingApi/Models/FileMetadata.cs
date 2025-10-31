using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FileHostingApi.Models
{
    public class FileMetadata
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("filename")]
        public string FileName { get; set; }

        [BsonElement("uploadDate")]
        public DateTime UploadDate { get; set; }

        [BsonElement("size")]
        public long Size { get; set; }

        [BsonElement("uploader")]
        public string Uploader { get; set; }
    }
}
