using DocumentVault.Models;
using DocumentVault.Services.BlobStorageService;
using DocumentVault.Services.CdnService;
using DocumentVault.Services.CosmosDbService;
using System.Text.Json;

namespace DocumentVault.Services.DocumentService
{
    public class DocumentService : IDocumentService
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ICdnService _cdnService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            IBlobStorageService blobStorageService,
            ICosmosDbService cosmosDbService,
            ILogger<DocumentService> logger,
            ICdnService cdnService)
        {
            _blobStorageService = blobStorageService;
            _cosmosDbService = cosmosDbService;
            _cdnService = cdnService;
            _logger = logger;
        }

        public async Task<bool> DeleteDocumentAsync(string id)
        {
            var document = await _cosmosDbService.GetDocumentByIdAsync(id);
            if (document == null) return false;

            try
            {
                if (document.CdnEnabled)
                {
                    await _cdnService.PurgeContentAsync(document.BlobName);
                    _logger.LogInformation("Purged CDN content for document {DocumentId}", id);
                }

                await _blobStorageService.DeleteFileAsync(document.BlobName);
                await _cosmosDbService.DeleteDocumentAsync(id);

                _logger.LogInformation("Document deleted successfully: {DocumentId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                throw;
            }
        }

        public async Task<Document?> GetDocumentByIdAsync(string id)
        {
            var document = await _cosmosDbService.GetDocumentByIdAsync(id);

            if (document != null && document.CdnEnabled)
            {
                document.CdnUrl = await _cdnService.GetCdnUrlAsync(document.BlobName);
            }

            return document;
        }

        public async Task<IEnumerable<Document>> GetDocumentsAsync()
        {
            var document = await _cosmosDbService.GetAllDocumentsAsync();

            var documentList = document.ToList();
            foreach (var item in documentList.Where(d => d.CdnEnabled))
            {
                item.CdnUrl = await _cdnService.GetCdnUrlAsync(item.BlobName);
            }
            return documentList;
        }

        public async Task<Document> UploadDocumentAsync(IFormFile file, List<string> tags, bool enableCdn = false)
        {
            var blobName = GenerateUniqueBlobName(file.FileName);
            var blobUrl = await _blobStorageService.UploadFileAsync(file, blobName);

            var document = new Document
            {
                id = Guid.NewGuid().ToString(),
                FileName = file.FileName,
                BlobName = blobName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                CdnEnabled = enableCdn,
                Tags = tags
            };

            if (enableCdn)
            {
                document.CdnUrl = await _cdnService.GetCdnUrlAsync(blobName);
                _logger.LogInformation("CDN enabled for uploaded document {DocumentId}", document.id);
            }

            Console.WriteLine($"Generated ID: {document.id}");
            Console.WriteLine($"Document before Cosmos: {System.Text.Json.JsonSerializer.Serialize(document)}");

            return await _cosmosDbService.CreateDocumentAsync(document);
        }

        private string GenerateUniqueBlobName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            return $"{Guid.NewGuid()}{extension}";
        }
    }
}