using DocumentVault.Models;
using DocumentVault.Services.BlobStorageService;
using DocumentVault.Services.CdnService;
using DocumentVault.Services.CosmosDbService;

namespace DocumentVault.Services.LinkService
{
    public class LinkService : ILinkService
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ICdnService _cdnService;
        private readonly ILogger<LinkService> _logger;

        public LinkService(
            ICosmosDbService cosmosDbService,
            IBlobStorageService blobStorageService,
            ICdnService cdnService,
            ILogger<LinkService> logger)
        {
            _cosmosDbService = cosmosDbService;
            _blobStorageService = blobStorageService;
            _cdnService = cdnService;
            _logger = logger;
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

            if (document.CdnEnabled)
            {
                downloadLink.DirectUrl = _cdnService.GenerateSecureUrl(document.BlobName, expiryHours);
                downloadLink.UseCdn = true;
                _logger.LogInformation("Generated CDN-based download link for document {DocumentId}", documentId);
            }
            else
            {
                downloadLink.UseCdn = false;
                _logger.LogInformation("Generated direct download link for document {DocumentId}", documentId);
            }

            var createdLink = await _cosmosDbService.CreateDownloadLinkAsync(downloadLink);
            _logger.LogInformation("Created download link {LinkId} for document {DocumentId}", createdLink.id, documentId);

            return createdLink;
        }

        public async Task<(Stream fileStream, string fileName, string contentType)?> DownloadFileAsync(string linkId)
        {
            var link = await _cosmosDbService.GetDownloadLinkAsync(linkId);
            if (link == null || link.IsExpired || !link.IsActive)
                return null;

            // Get document info
            var document = await _cosmosDbService.GetDocumentByIdAsync(link.DocumentId);
            if (document == null)
                return null;

            try
            {
                // Update download count
                await _cosmosDbService.UpdateDownloadLinkAsync(link);

                // Get file stream - CDN first, then blob storage fallback
                var fileStream = document.CdnEnabled
                    ? await TryDownloadFromCdnAsync(document, link) ?? await _blobStorageService.DownloadFileAsync(document.BlobName)
                    : await _blobStorageService.DownloadFileAsync(document.BlobName);

                return (fileStream, document.FileName, document.ContentType);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ValidateLinkAsync(string linkId)
        {
            var link = await _cosmosDbService.GetDownloadLinkAsync(linkId);
            return link != null && !link.IsExpired && link.IsActive;
        }

        private async Task<Stream?> TryDownloadFromCdnAsync(Document document, DownloadLink link)
        {
            try
            {
                // Try stored direct URL first, then document CDN URL, then generate new one
                var cdnUrl = link.DirectUrl
                          ?? document.CdnUrl
                          ?? _cdnService.GenerateSecureUrl(document.BlobName, (int)Math.Ceiling((link.ExpiresAt - DateTime.UtcNow).TotalHours));

                if (string.IsNullOrEmpty(cdnUrl)) return null;

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var response = await httpClient.GetAsync(cdnUrl);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
            catch
            {
                return null; // Will fallback to blob storage
            }
        }



        public async Task<IEnumerable<DownloadLink>> GetAllLinksAsync()
        {
            return await _cosmosDbService.GetAllDownloadLinksAsync();
        }
    }
}