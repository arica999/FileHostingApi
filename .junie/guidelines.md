# FileUploader — Project Development Guidelines

These notes capture project-specific details to accelerate development and debugging for advanced contributors.

## 1) Build, Run, and Configuration

- Target framework: `net8.0`
- Minimal hosting model (single `Program.cs`). A `partial Program` is declared to support `WebApplicationFactory` for integration tests.
- Swagger is enabled in Development only.
- HTTPS redirection is disabled to simplify TestServer/integration execution in CI.

### 1.1 Build

- Full solution build:
  - `dotnet build FileUploader.sln -c Debug`

### 1.2 Run API locally

- Development run from the API project directory:
  - `dotnet run --project src/FileHostingApi/FileHostingApi.csproj`
- Swagger UI is available (Development only): `http://localhost:<port>/swagger`
- Healthcheck endpoint: `GET /health` → returns plain text: `API works`

### 1.3 File storage layout

- Uploaded files are stored under `<contentRoot>/UploadedFiles` where `contentRoot` is the app’s current directory at runtime. The controller guarantees the directory exists.
- In integration tests the content root resolves under the test host’s working directory (typically the test project’s `bin/<cfg>/<tfm>`). Tests therefore do not require elevated permissions.

### 1.4 Metadata persistence

- Primary persistence is MongoDB via `MongoDB.Driver`.
- Class `FileHostingApi.Services.FileMetadataService` supports two modes at runtime:
  1) Mongo-backed: Active when both configuration keys are present and non-empty:
     - `MongoDbSettings:ConnectionString`
     - `MongoDbSettings:Database`
  2) In-memory fallback: Used automatically when the above settings are absent or the driver initialization fails. This mode is deterministic and isolated per-process, suitable for local testing and CI without a Mongo instance.

- Production deployments should provide valid Mongo settings. A `docker-compose.yml` is provided to run a local Mongo container.

### 1.5 MongoDB via Docker (optional)

- From `src/FileHostingApi` you can spin up Mongo and the API:
  - `docker compose -f src/FileHostingApi/docker-compose.yml up --build`
- Expected effective environment:
  - Mongo exposed on `localhost:27017`
  - API exposed on `localhost:5000`
- If you want the API to use Mongo when running outside Compose, provide:
  - Windows PowerShell example:
    - `$env:MongoDbSettings__ConnectionString = "mongodb://localhost:27017"`
    - `$env:MongoDbSettings__Database = "FileHostingDb"`
    - `dotnet run --project src/FileHostingApi/FileHostingApi.csproj`


## 2) Testing

The repository includes an integration test suite using `Microsoft.AspNetCore.Mvc.Testing` and `xUnit` located at `tests/FileHostingApi.Tests`.

- The tests start the API in-process via `WebApplicationFactory<FileHostingApi.Program>`.
- The test host defaults to the `Production` environment unless overridden; our API remains test-friendly in that environment (Swagger disabled; HTTPS redirection disabled; metadata falls back to in-memory store when Mongo is not configured).

### 2.1 Run all tests

- From the solution root:
  - `dotnet test -c Debug`

### 2.2 Run a specific test (verified)

- Example: run only the health check test (this currently passes):
  - `dotnet test -c Debug --filter FullyQualifiedName~FileHostingApi.Tests.FileHostingApiTests.HealthCheck_ReturnsOk`
- Expected behavior:
  - Status: Passed
  - The test calls `GET /health` and asserts the body contains `API works`.

### 2.3 Integration flow tests (notes)

- The suite also contains end-to-end tests that exercise file upload/list/download/delete. These use multipart form-data to upload content and validate the responses.
- When `MongoDbSettings` are not provided, tests use the in-memory metadata store; uploaded file bytes are persisted under the host’s `UploadedFiles` directory.

### 2.4 Adding new tests

- Create new test classes in `tests/FileHostingApi.Tests` and use the existing fixture pattern:
  - `public class MyTests : IClassFixture<WebApplicationFactory<FileHostingApi.Program>> { ... }`
- For HTTP calls, use `var client = _factory.CreateClient();` and standard `HttpClient` calls.
- Example template (minimal):

```csharp
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FileHostingApi.Tests
{
    public class SmokeTests : IClassFixture<WebApplicationFactory<FileHostingApi.Program>>
    {
        private readonly WebApplicationFactory<FileHostingApi.Program> _factory;
        public SmokeTests(WebApplicationFactory<FileHostingApi.Program> factory) => _factory = factory;

        [Fact]
        public async Task Health_ReturnsOk_And_EnglishMessage()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/health");
            var content = await resp.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Contains("API works", content);
        }
    }
}
```

- To run just this test:
  - `dotnet test --filter FullyQualifiedName~FileHostingApi.Tests.SmokeTests.Health_ReturnsOk_And_EnglishMessage`

### 2.5 Common test pitfalls specific to this project

- Environment-dependent responses: Healthcheck response is intentionally plain English (`API works`) as tests assert this exact string.
- Content root and file I/O: Uploads write under `UploadedFiles` in the current working directory of the host. Avoid relying on absolute paths in tests; let the app manage paths.
- Mongo configuration: If `MongoDbSettings` are provided but Mongo is not reachable, the driver may throw on first operation. For test reliability, either leave the settings unset (to use in-memory) or run Mongo locally (see 1.5).


## 3) Development Notes (Project-Specific)

### 3.1 Code style and conventions

- C# 12 / .NET 8 features allowed; implicit usings and nullable reference types are enabled in the API project.
- Keep controller actions concise, return proper status codes, and rely on the service layer for persistence logic.
- The model `FileMetadata` uses MongoDB BSON attributes for the Mongo-backed mode; the in-memory mode treats the same structure as a plain POCO.
- When creating new metadata entries outside Mongo, use string IDs. The service currently assigns IDs using `MongoDB.Bson.ObjectId.GenerateNewId().ToString()` for consistency.

### 3.2 Dependency injection and environments

- `FileMetadataService` is registered as a singleton. It switches between Mongo and in-memory based on the presence of configuration keys; no test-time substitution is required.
- Swagger is restricted to Development. The test server typically runs in Production; do not rely on Swagger being available in tests.

### 3.3 API surface and routes

- Health: `GET /health`
- File APIs (controller base route `api/file`):
  - `POST /api/file/upload` — multipart form-data (`File`, `Uploader`)
  - `GET /api/file/list` — returns JSON array of `FileMetadata`
  - `GET /api/file/download/{id}` — download by ID
  - `GET /api/file/download/byname/{filename}` — download by filename
  - `DELETE /api/file/{id}` — delete file and metadata

### 3.4 Debugging tips

- If a test returns 500, check whether `MongoDbSettings` are being injected unintentionally in your environment. Unset them to trigger the in-memory mode for isolation.
- To force Development behavior locally (e.g., Swagger and detailed errors):
  - PowerShell: `$env:ASPNETCORE_ENVIRONMENT = "Development"`
- Add targeted logging around controller actions when diagnosing file system issues (path resolution, permissions).


## 4) Verified commands snapshot

The following were executed and verified during guideline preparation on this codebase:
- `dotnet build FileUploader.sln -c Debug`
- `dotnet test -c Debug --filter FullyQualifiedName~FileHostingApi.Tests.FileHostingApiTests.HealthCheck_ReturnsOk` → Passed
- `dotnet test -c Debug` currently reports failures in the full CRUD flow; see 3.4 for debugging pointers if working on that area.


## 5) Housekeeping

- Uploaded files produced by tests are written under the test host’s `UploadedFiles` folder; they are transient under `bin/` and don’t require explicit cleanup.
- No extra files are required to be kept beyond repository contents.
