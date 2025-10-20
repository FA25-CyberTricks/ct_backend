namespace ct.backend.ExternalServices.Storage
{
    public class GoogleStorageSettings
    {
        public string Bucket { get; set; } = string.Empty;
        public string? KeyPath { get; set; }
    }
}
