namespace DocumentVault.Services.BlobStorageService
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string blobName);
        Task<Stream> DownloadFileAsync(string blobName);
        Task<bool> DeleteFileAsync(string blobName);
        Task<bool> BlobExistsAsync(string blobName);
    }
}
