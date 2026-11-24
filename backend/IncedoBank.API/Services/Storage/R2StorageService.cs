// Services/Storage/R2StorageService.cs
// Cloudflare R2 storage (S3-compatible, zero egress fees, 10GB free)

using Amazon.S3;
using Amazon.S3.Model;

namespace IncedoBank.API.Services.Storage;

public interface IStorageService
{
    Task<string> GenerateUploadUrlAsync(string objectKey, string contentType, int expiryMinutes = 15, CancellationToken ct = default);
    Task<string> GenerateDownloadUrlAsync(string objectKey, int expiryMinutes = 60, CancellationToken ct = default);
    Task DeleteObjectAsync(string objectKey, CancellationToken ct = default);
}

public class R2StorageService(IAmazonS3 s3Client, IConfiguration config, ILogger<R2StorageService> logger) : IStorageService
{
    private string BucketName => config["Cloudflare:R2:BucketName"] ?? throw new InvalidOperationException("R2 bucket not configured");

    public async Task<string> GenerateUploadUrlAsync(
        string objectKey, string contentType, int expiryMinutes = 15, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            ContentType = contentType
        };

        var url = s3Client.GetPreSignedURL(request);
        logger.LogDebug("Generated R2 upload URL for {ObjectKey}", objectKey);
        return url;
    }

    public async Task<string> GenerateDownloadUrlAsync(
        string objectKey, int expiryMinutes = 60, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };

        return s3Client.GetPreSignedURL(request);
    }

    public async Task DeleteObjectAsync(string objectKey, CancellationToken ct = default)
    {
        await s3Client.DeleteObjectAsync(BucketName, objectKey, ct);
        logger.LogInformation("Deleted R2 object {ObjectKey}", objectKey);
    }
}
