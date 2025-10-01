namespace DocumentVault.dtos
{
    public class LinkRequestDto
    {
        public string DocumentId { get; set; }
        public int ExpiryHours { get; set; } = 1;
    }
}