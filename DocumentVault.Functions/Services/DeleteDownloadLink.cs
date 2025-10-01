using DocumentVault.Functions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentVault.Functions.Services
{
    public class DeleteDownloadLink : IDeleteDownloadLink
    {
        private readonly Container _container;
        private readonly ILogger<DeleteDownloadLink> _logger;

        public DeleteDownloadLink(
            CosmosClient cosmosClient,
            IConfiguration configuration,
            ILogger<DeleteDownloadLink> logger)
        {
            var databaseName = configuration["CosmosDB_DatabaseName"];
            var containerName = configuration["CosmosDB_ContainerName"];
            _container = cosmosClient.GetContainer(databaseName, containerName);
            _logger = logger;
        }

        public async Task<IEnumerable<DownloadLink>> GetExpiredLinksAsync()
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation("Checking for expired links at {Now}", now);

            var query = "SELECT * FROM c WHERE c.partitionKey = 'links' AND c.ExpiresAt < @now";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@now", now); 

            var queryIterator = _container.GetItemQueryIterator<DownloadLink>(queryDefinition);
            var expiredLinks = new List<DownloadLink>();

            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                expiredLinks.AddRange(response);
            }

            _logger.LogInformation("Query found {Count} expired links", expiredLinks.Count);

            return expiredLinks;
        }

        public async Task<int> DeleteAllExpiredLinksAsync()
        {
            var expiredLinks = await GetExpiredLinksAsync();
            int deletedCount = 0;

            _logger.LogInformation("Starting deletion of {Count} expired links", expiredLinks.Count());

            foreach (var link in expiredLinks)
            {
                try
                {
                    _logger.LogInformation("Deleting link {LinkId} (expired at {ExpiresAt})", link.id, link.ExpiresAt);

                    await _container.DeleteItemAsync<DownloadLink>(link.id, new PartitionKey("links"));
                    deletedCount++;

                    _logger.LogInformation("Successfully deleted link {LinkId}", link.id);
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Link {LinkId} not found (may have been deleted already)", link.id);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete link {LinkId}", link.id);
                }
            }

            _logger.LogInformation("Deletion complete: {DeletedCount} of {TotalCount} links deleted",
                deletedCount, expiredLinks.Count());

            return deletedCount;
        }
    }
}