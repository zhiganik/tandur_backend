using Amazon.S3;
using Amazon.S3.Model;
using Core.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class R2StorageService : IStorageService, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string         _bucket;
    private readonly string         _baseUrl;

    public R2StorageService(IOptions<R2Options> options)
    {
        var o = options.Value;
        _bucket  = o.BucketName;
        _baseUrl = o.PublicBaseUrl.TrimEnd('/');

        _client = new AmazonS3Client(
            o.AccessKeyId,
            o.SecretAccessKey,
            new AmazonS3Config
            {
                ServiceURL    = $"https://{o.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
            });
    }

    public async Task<string> UploadAsync(string key, Stream stream, string contentType)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName  = _bucket,
            Key         = key,
            InputStream = stream,
            ContentType = contentType,
        });

        return $"{_baseUrl}/{key}";
    }

    public Task DeleteAsync(string key) =>
        _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key        = key,
        });

    public string ExtractKey(string publicUrl) =>
        publicUrl.StartsWith(_baseUrl)
            ? publicUrl[(_baseUrl.Length + 1)..]
            : publicUrl;

    public void Dispose() => _client.Dispose();
}
