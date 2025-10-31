using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FileHostingApi.Tests
{
    // Full suite of integration tests for the file hosting API
    public class FileHostingApiTests : IClassFixture<WebApplicationFactory<FileHostingApi.Program>>
    {
        private readonly WebApplicationFactory<FileHostingApi.Program> _factory;

        public FileHostingApiTests(WebApplicationFactory<FileHostingApi.Program> factory)
        {
            _factory = factory;
        }

        [Fact(DisplayName = "HealthCheck: GET /health")]
        public async Task HealthCheck_ReturnsOk()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/health");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("API works", content);
        }

        [Fact(DisplayName = "File list is empty initially")]
        public async Task FileList_IsEmptyInitially()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/file/list");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.True(content.Contains("[]") || content.Length < 5); // List should be empty
        }

        [Fact(DisplayName = "CRUD: full file lifecycle (upload, list, download, delete)")]
        public async Task File_FullCrudCycle_WorksCorrectly()
        {
            var client = _factory.CreateClient();

            // --- Upload ---
            var testFileContent = "This is a test file!";
            var testFile = new ByteArrayContent(Encoding.UTF8.GetBytes(testFileContent));
            testFile.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

            var multi = new MultipartFormDataContent();
            multi.Add(testFile, "File", "upload_test.txt");
            multi.Add(new StringContent("Tester"), "Uploader");

            var uploadResp = await client.PostAsync("/api/file/upload", multi);
            uploadResp.EnsureSuccessStatusCode();
            var uploadJson = JObject.Parse(await uploadResp.Content.ReadAsStringAsync());
            string id = uploadJson["metadata"]["id"]?.ToString();
            string filename = uploadJson["metadata"]["fileName"]?.ToString();

            Assert.False(string.IsNullOrWhiteSpace(id));
            Assert.Equal("upload_test.txt", filename);

            // --- List: should contain 1 file ---
            var listResp = await client.GetAsync("/api/file/list");
            listResp.EnsureSuccessStatusCode();
            var listJson = JArray.Parse(await listResp.Content.ReadAsStringAsync());
            Assert.Single(listJson);

            // --- Download by ID ---
            var downloadById = await client.GetAsync($"/api/file/download/{id}");
            downloadById.EnsureSuccessStatusCode();
            var downloadedContent = await downloadById.Content.ReadAsStringAsync();
            Assert.Equal(testFileContent, downloadedContent);

            // --- Download by name ---
            var downloadByName = await client.GetAsync($"/api/file/download/byname/upload_test.txt");
            downloadByName.EnsureSuccessStatusCode();
            var downloadedByName = await downloadByName.Content.ReadAsStringAsync();
            Assert.Equal(testFileContent, downloadedByName);

            // --- Delete by ID ---
            var deleteResp = await client.DeleteAsync($"/api/file/{id}");
            deleteResp.EnsureSuccessStatusCode();

            // --- After delete the list should be empty again ---
            var postDeleteList = await client.GetAsync("/api/file/list");
            postDeleteList.EnsureSuccessStatusCode();
            var postDeleteContent = await postDeleteList.Content.ReadAsStringAsync();
            Assert.True(postDeleteContent.Contains("[]") || postDeleteContent.Length < 5);
        }
    }
}
