using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace DocumentVault.Services.BlobStorageService
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            var containerName = configuration["Azure:BlobStorage:ContainerName"];
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Create container if it doesn't exist (for local development)
            try
            {
                _containerClient.CreateIfNotExistsAsync(PublicAccessType.None).GetAwaiter().GetResult();
                Console.WriteLine($"Container '{containerName}' is ready.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create container '{containerName}': {ex.Message}");
            }
        }

        public async Task<bool> BlobExistsAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.ExistsAsync();
            return response.Value;
        }

        public async Task<bool> DeleteFileAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }

        public async Task<Stream> DownloadFileAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            return blobClient.Uri.ToString();
        }
    }
}