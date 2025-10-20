namespace ct.backend.Common.Ports.Storage
{
    public interface IGoogleStorageService
    {
        string GetDefaultAvatar();
        Task<string> UploadAsync(Stream stream, string objectName, string contentType, CancellationToken ct = default);
        string GetPublicUrl(string objectName);
        string GetSignedReadUrl(string objectName, TimeSpan ttl);
        string GetSignedWriteUrl(string objectName, string contentType, TimeSpan ttl);
        string GetSignedResumableInitiationUrl(string objectName, string contentType, TimeSpan ttl);
        Task<bool> DeleteAsync(string objectName, CancellationToken ct = default);
    }
}
