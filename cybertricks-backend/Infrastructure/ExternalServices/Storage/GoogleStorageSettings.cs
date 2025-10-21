namespace ct.backend.Infrastructure.ExternalServices.Storage
{
    public class GoogleStorageSettings
    {
        public string Bucket { get; set; } = string.Empty;
        public string? KeyPath { get; set; }
    }
}
