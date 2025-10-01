using DocumentVault.Functions.Models;

namespace DocumentVault.Functions.Services
{
    public interface IDeleteDownloadLink
    {
        Task<IEnumerable<DownloadLink>> GetExpiredLinksAsync();
        Task<int> DeleteAllExpiredLinksAsync();
    }
}