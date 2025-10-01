using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using DocumentVault.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace DocumentVault.Services.CdnService
{
    public class CdnService : ICdnService
    {
        private readonly CdnOptions _cdnOptions;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<CdnService> _logger;

        public CdnService(IOptions<CdnOptions> cdnOptions, ILogger<CdnService> logger, BlobServiceClient blobServiceClient)
        {
            _cdnOptions = cdnOptions.Value;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        public string GenerateSecureUrl(string blobName, int expiryHours)
        {
            try
            {

                var blobClient = _blobServiceClient.GetBlobContainerClient("documents")
                    .GetBlobClient(blobName);

                var sasBuilder = new BlobSasBuilder
                {

                    BlobContainerName = "documents",
                    BlobName = blobName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), 
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                var sasUri = blobClient.GenerateSasUri(sasBuilder);

                if (_cdnOptions.EnableCdn && !string.IsNullOrEmpty(_cdnOptions.CdnEndpoint))
                {
                    return sasUri.ToString().Replace(
                        $"https://{_blobServiceClient.AccountName}.blob.core.windows.net",
                        $"https://{_cdnOptions.CdnEndpoint}"
                    );
                }
                return sasUri.ToString();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error generating secure SAS URL for {BlobName}", blobName);
                return string.Empty;
            }
        }

        public async Task<string> GetCdnUrlAsync(string blobName)
        {
            return GenerateSecureUrl(blobName, 0);
        }

        public async Task<bool> PurgeContentAsync(string blobName)
        {
            try
            {
                if (!_cdnOptions.EnableCdn)
                    return true;

                _logger.LogInformation("CDN purge requested for {BlobName}. Manual purge required in Azure Portal.", blobName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CDN purge request for {BlobName}", blobName);
                return false;
            }
        }

   
    }
}