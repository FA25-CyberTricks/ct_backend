using ct.backend.Common.Constants;
using ct.backend.Common.Ports.Storage;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ct.backend.ExternalServices.Storage
{
    internal sealed class GoogleStorageService : IGoogleStorageService
    {
        private readonly ILogger<GoogleStorageService> _logger = NullLogger<GoogleStorageService>.Instance;
        private readonly StorageClient _client;
        private readonly UrlSigner _signer;
        private readonly string _bucket;

        public GoogleStorageService(StorageClient client, UrlSigner signer, IOptions<GoogleStorageSettings> opt)
        {
            _logger = NullLogger<GoogleStorageService>.Instance;
            _client = client;
            _signer = signer;
            _bucket = opt.Value.Bucket ?? throw new ArgumentNullException(nameof(opt.Value.Bucket));
        }

        public string GetDefaultAvatar()
        {
            return $"https://storage.googleapis.com/{_bucket}/{GoogleStoragePaths.DefaultAvatars}";
        }

        public async Task<string> UploadAsync(Stream stream, string objectName, string contentType, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("Object name is required.", nameof(objectName));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content-Type is required.", nameof(contentType));

            if (stream.CanSeek) stream.Position = 0;

            var obj = await _client.UploadObjectAsync(
                bucket: _bucket,
                objectName: objectName,
                contentType: contentType,
                source: stream,
                // options: new UploadObjectOptions { CacheControl = "public, max-age=31536000" },
                cancellationToken: ct);

            return obj.Name;
        }

        public string GetPublicUrl(string objectName)
            => $"https://storage.googleapis.com/{_bucket}/{Uri.EscapeDataString(objectName)}";

        public string GetSignedReadUrl(string objectName, TimeSpan ttl)
        {
            if (ttl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ttl));

            return _signer.Sign(_bucket, objectName, ttl, HttpMethod.Get);
        }

        public string GetSignedWriteUrl(string objectName, string contentType, TimeSpan ttl)
        {
            if (ttl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ttl));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content-Type is required.", nameof(contentType));

            var template = UrlSigner.RequestTemplate
                .FromBucket(_bucket)
                .WithObjectName(objectName)
                .WithHttpMethod(HttpMethod.Put)
                .WithContentHeaders(new[]
                {
                    new KeyValuePair<string, IEnumerable<string>>("Content-Type", new[] { contentType })
                });

            var options = UrlSigner.Options.FromDuration(ttl);
            return _signer.Sign(template, options);
        }

        public string GetSignedResumableInitiationUrl(string objectName, string contentType, TimeSpan ttl)
        {
            if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl));
            if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("Content-Type is required.", nameof(contentType));

            var template = UrlSigner.RequestTemplate
                .FromBucket(_bucket)
                .WithObjectName(objectName)
                // Quan trọng: dùng "ResumableHttpMethod" để ký POST + header x-goog-resumable:start
                .WithHttpMethod(UrlSigner.ResumableHttpMethod)
                .WithContentHeaders(new[]
                {
            new KeyValuePair<string, IEnumerable<string>>("Content-Type", new[] { contentType })
                });

            var options = UrlSigner.Options.FromDuration(ttl);
            return _signer.Sign(template, options);
        }

        // Not Implemented
        public async Task<bool> DeleteAsync(string objectName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("objectName is required.", nameof(objectName));

            objectName = NormalizeObjectName(objectName);

            try
            {
                await _client.DeleteObjectAsync(_bucket, objectName, cancellationToken: ct);
                return true; // existed & deleted
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation($"GCS delete: not found. bucket={_bucket}, name={objectName}");
                return false; // didn't exist
            }
        }

        private string NormalizeObjectName(string input)
        {
            var s = input.Trim();

            // Nếu là URL thì parse để lấy phần sau bucket
            // ví dụ: https://storage.googleapis.com/lecx-storage/public/user-avatars/a.png
            if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
                {
                    // Path = /lecx-storage/public/user-avatars/a.png  hoặc /public/user-avatars/a.png (tuỳ dạng URL)
                    var path = uri.AbsolutePath.Trim('/');
                    // Nếu path bắt đầu bằng bucket → cắt bỏ
                    if (path.StartsWith(_bucket + "/", StringComparison.Ordinal))
                        path = path.Substring(_bucket.Length + 1);
                    s = path;
                }
            }

            // Bỏ leading slash nếu có
            s = s.Trim().TrimStart('/');
            return s;
        }
    }
}
