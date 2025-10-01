using DocumentVault.Models;

namespace DocumentVault.Services.CdnService
{
    public interface ICdnService
    {
        Task<string> GetCdnUrlAsync(string blobName);
        string GenerateSecureUrl(string blobName, int expiryHours);
        Task<bool> PurgeContentAsync(string blobName);
    }
}