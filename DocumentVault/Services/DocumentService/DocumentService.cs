using DocumentVault.Models;
using DocumentVault.Services.BlobStorageService;
using DocumentVault.Services.CosmosDbService;
using System.Text.Json;

namespace DocumentVault.Services.DocumentService
{
    public class DocumentService : IDocumentService
    { 
        private readonly IBlobStorageService _blobStorageService;
        private readonly ICosmosDbService _cosmosDbService;


        public DocumentService(IBlobStorageService blobStorageService, ICosmosDbService cosmosDbService)
        {
            _blobStorageService = blobStorageService;
            _cosmosDbService = cosmosDbService;
        }
        public async Task<bool> DeleteDocumentAsync(string id)
        {
            var document = await _cosmosDbService.GetDocumentByIdAsync(id);
            if (document == null) return false;

            await _blobStorageService.DeleteFileAsync(document.BlobName);
            await _cosmosDbService.DeleteDocumentAsync(id);
            return true;

        }

        public async  Task<Document?> GetDocumentByIdAsync(string id)
        {
            return await _cosmosDbService.GetDocumentByIdAsync(id);
        }

        public async Task<IEnumerable<Document>> GetDocumentsAsync()
        {
            return await _cosmosDbService.GetAllDocumentsAsync();
        }

        public async Task<Document> UploadDocumentAsync(IFormFile file, List<string> tags)
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
                Tags = tags
            };
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
