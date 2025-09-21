using System.Text.Json.Serialization;

public class DownloadLink
{
    
    public string id { get; set; } 

   
    public string DocumentId { get; set; }

    public DateTime ExpiresAt { get; set; }

 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;


    public string partitionKey { get; set; } = "links";
}