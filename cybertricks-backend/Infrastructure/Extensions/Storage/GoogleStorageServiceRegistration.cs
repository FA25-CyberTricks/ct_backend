using ct.backend.Common.Ports.Storage;
using ct.backend.ExternalServices.Storage;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace ct.backend.Extensions.GoogleStorage;

public static class GoogleStorageRegistration
{
    [Obsolete]
    public static IServiceCollection AddGoogleStorage(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Bind setting (Bucket, DefaultAvatarPath, KeyPath)
        services.Configure<GoogleStorageSettings>(config.GetSection("GcpStorage"));

        // GoogleCredential (auto dùng ADC nếu không có KeyPath)
        services.AddSingleton(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<GoogleStorageSettings>>().Value;
            var cred = string.IsNullOrWhiteSpace(opt.KeyPath)
                ? GoogleCredential.GetApplicationDefault()
                : GoogleCredential.FromFile(opt.KeyPath);
            return StorageClient.Create(cred);
        });

        // UrlSigner
        services.AddSingleton(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<GoogleStorageSettings>>().Value;
            var keyPath = opt.KeyPath ?? throw new("Missing GcpStorage:KeyPath for UrlSigner");
            var sa = ServiceAccountCredential.FromServiceAccountData(File.OpenRead(keyPath));
            return UrlSigner.FromServiceAccountCredential(sa);
        });

        // Service triển khai
        services.AddScoped<IGoogleStorageService, GoogleStorageService>();

        return services;
    }
}
