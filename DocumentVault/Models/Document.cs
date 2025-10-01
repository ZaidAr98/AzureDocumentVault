using System.Text.Json.Serialization;

namespace DocumentVault.Models
{
    public class Document
    {

      
        public string id { get; set; } 

    
        public string FileName { get; set; }

       
        public string BlobName { get; set; }


        public string ContentType { get; set; }


    
        public long FileSize { get; set; }
      
        
        
    
        public List<string> Tags { get; set; } = new List<string>();

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public string partitionKey { get; set; } = "documents";

        public string? CdnUrl { get; set; }

        public bool CdnEnabled { get; set; } = true;

    }
}
