namespace Core.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(string key, Stream stream, string contentType);
    Task DeleteAsync(string key);
    string ExtractKey(string publicUrl);
}
