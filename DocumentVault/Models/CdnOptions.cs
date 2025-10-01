namespace DocumentVault.Models
{
    public class CdnOptions
    {
        public const string sectionName = "Cdn";
        public string BaseUrl { get; set; } = string.Empty;

        public string CdnEndpoint { get; set; } = string.Empty;

        public string SecurityKey { get; set; } = string.Empty;

        public bool EnableCdn { get; set; } = false;

        public int DefaultCacheTtlMinutes { get; set; } = 60;
    }
}
