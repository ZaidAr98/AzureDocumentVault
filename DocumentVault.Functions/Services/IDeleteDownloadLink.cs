namespace DocumentVault.Functions.Services
{
     public interface IDeleteDownloadLink
    {
        Task<int> DeleteAllExpiredLinksAsync();
    }
}
