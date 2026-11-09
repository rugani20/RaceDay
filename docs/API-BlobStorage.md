# Adding Azure Blob Storage to the Part 2 API (requirement 5)

The MVC app never talks to Azure. It posts the image to the API, and the API stores it in Blob Storage.

## 1. Package and config
```
dotnet add package Azure.Storage.Blobs
```
appsettings.json (use user-secrets or an env variable for the real value, never commit it):
```json
"AzureBlob": { "ConnectionString": "UseDevelopmentStorage=true", "Container": "raceday-images" }
```
(`UseDevelopmentStorage=true` works with the Azurite emulator for local testing.)

## 2. Service (Services/BlobStorageService.cs)
```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public interface IBlobStorageService { Task<string> UploadAsync(IFormFile file, string folder); }

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;
    public BlobStorageService(IConfiguration cfg)
    {
        _container = new BlobContainerClient(cfg["AzureBlob:ConnectionString"], cfg["AzureBlob:Container"]);
        _container.CreateIfNotExists(PublicAccessType.Blob);   // images readable by URL
    }
    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var name = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var blob = _container.GetBlobClient(name);
        await using var s = file.OpenReadStream();
        await blob.UploadAsync(s, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }
}
```
Register in Program.cs: `builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();`

## 3. Endpoints the MVC expects
Events POST/PUT accept `[FromForm]` with an optional `IFormFile banner`:
```csharp
[HttpPost] public async Task<IActionResult> Create([FromForm] EventForm dto, IFormFile? banner)
{
    var ev = new Event { /* map fields */ };
    if (banner != null) ev.BannerUrl = await _blob.UploadAsync(banner, "banners");
    ...
}
[HttpPost("{id}/profile-picture")]   // on UsersController
public async Task<IActionResult> Picture(int id, IFormFile file) { user.ProfilePictureUrl = await _blob.UploadAsync(file, "profiles"); ... }
```
Add `BannerUrl` and `ProfilePictureUrl` (nvarchar, nullable) to Events and Users, then add an EF migration.
