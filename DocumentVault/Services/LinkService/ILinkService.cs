namespace DocumentVault.Services.LinkService
{
    public interface ILinkService
    {
        Task<DownloadLink> GenerateLinkAsync(string documentId, int expiryHours);
        Task<(Stream fileStream, string fileName, string contentType)?> DownloadFileAsync(string linkId);
        Task<bool> ValidateLinkAsync(string linkId);
        Task<IEnumerable<DownloadLink>> GetAllLinksAsync();
    }
}
