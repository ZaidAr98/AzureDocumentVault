namespace DocumentVault.dtos
{
    public class LinkResponseDto
    {
        public string LinkId { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime ExpiresAt { get; set; }

        public string DocumentName { get; set; }

        public string? DirectUrl { get; set; }

        public bool UseCdn { get; set; } = false;

    }
}
