namespace DocumentVault.dtos
{
    public class UploadRequestDto
    {
        public IFormFile File { get; set; }
        public List<string> Tags { get; set; } = new List<string>();


         public bool EnableCdn { get; set; } = false;

    }
}
