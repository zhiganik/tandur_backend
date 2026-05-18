using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Services;

/// <summary>
/// Dev-only storage service that saves files to wwwroot/uploads/.
/// Register instead of R2StorageService when R2 credentials are not available.
/// </summary>
public class LocalStorageService(IWebHostEnvironment env) : IStorageService
{
    private readonly string _uploadsPath = Path.Combine(env.WebRootPath ?? env.ContentRootPath, "uploads");
    private readonly string _baseUrl     = "http://localhost:5280/uploads";

    public async Task<string> UploadAsync(string key, Stream stream, string contentType)
    {
        var filePath = Path.Combine(_uploadsPath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs);

        return $"{_baseUrl}/{key}";
    }

    public Task DeleteAsync(string key)
    {
        var filePath = Path.Combine(_uploadsPath, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    public string ExtractKey(string publicUrl) =>
        publicUrl.StartsWith(_baseUrl)
            ? publicUrl[(_baseUrl.Length + 1)..]
            : publicUrl;
}
