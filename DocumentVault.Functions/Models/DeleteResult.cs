

namespace DocumentVault.Functions.Models
{
  
        public class DeleteResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? Error { get; set; }
            public int DeletedCount { get; set; }
        }
    
}
