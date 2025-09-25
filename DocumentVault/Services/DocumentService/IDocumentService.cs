using DocumentVault.Models;

namespace DocumentVault.Services.DocumentService
{
    public interface IDocumentService
    {
        Task<Document> UploadDocumentAsync(IFormFile file, List<string> tags);
        Task<IEnumerable<Document>> GetDocumentsAsync();
        Task<Document?> GetDocumentByIdAsync(string id);
        Task<bool> DeleteDocumentAsync(string id);
     
    }
}
