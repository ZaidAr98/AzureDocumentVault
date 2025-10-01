namespace DocumentVault.Functions.Models
{
    public class DownloadLink
    {
        public string id { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool UseCdn { get; set; } = false;  
        public string? DirectUrl { get; set; }      
        public string partitionKey { get; set; } = "links";
    }
}