using DocumentVault.Models;
using DocumentVault.Services.BlobStorageService;
using DocumentVault.Services.CosmosDbService;

namespace DocumentVault.Services.LinkService
{
   
   

    public class LinkService : ILinkService
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IBlobStorageService _blobStorageService;

        public LinkService(ICosmosDbService cosmosDbService, IBlobStorageService blobStorageService)
        {
            _cosmosDbService = cosmosDbService;
            _blobStorageService = blobStorageService;
        }

        public async Task<DownloadLink> GenerateLinkAsync(string documentId, int expiryHours)
        {
            // Validate document exists
            var document = await _cosmosDbService.GetDocumentByIdAsync(documentId);
            if (document == null)
                throw new ArgumentException("Document not found");

            // Create download link
            var downloadLink = new DownloadLink
            {
                id = Guid.NewGuid().ToString(), 
                DocumentId = documentId,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            };

            // Save to database
            return await _cosmosDbService.CreateDownloadLinkAsync(downloadLink);
        }

        public async Task<(Stream fileStream, string fileName, string contentType)?> DownloadFileAsync(string linkId)
        {
            // Get and validate link
            var link = await _cosmosDbService.GetDownloadLinkAsync(linkId);
            if (link == null || link.IsExpired || !link.IsActive)
                return null;

            // Get document info
            var document = await _cosmosDbService.GetDocumentByIdAsync(link.DocumentId);
            if (document == null)
                return null;

            // Update download count
          
            await _cosmosDbService.UpdateDownloadLinkAsync(link);

            // Get file stream
            var fileStream = await _blobStorageService.DownloadFileAsync(document.BlobName);

            return (fileStream, document.FileName, document.ContentType);
        }

        public async Task<bool> ValidateLinkAsync(string linkId)
        {
            var link = await _cosmosDbService.GetDownloadLinkAsync(linkId);
            return link != null && !link.IsExpired && link.IsActive;
        }
    }
}