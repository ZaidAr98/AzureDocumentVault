using DocumentVault.Models;

namespace DocumentVault.Services.CosmosDbService
{
    public interface ICosmosDbService
    {
        Task<Document> CreateDocumentAsync(Document document);
        Task<Document?> GetDocumentByIdAsync(string id);
        Task<IEnumerable<Document>> GetAllDocumentsAsync();
        Task<IEnumerable<Document>> GetDocumentsByTagsAsync(List<string> tags);
        Task<bool> DeleteDocumentAsync(string id);
        Task<DownloadLink> CreateDownloadLinkAsync(DownloadLink downloadLink);
        Task<DownloadLink?> GetDownloadLinkAsync(string linkId);
        Task<bool> UpdateDownloadLinkAsync(DownloadLink downloadLink);
        Task<IEnumerable<DownloadLink>> GetAllDownloadLinksAsync();  
    }
}
