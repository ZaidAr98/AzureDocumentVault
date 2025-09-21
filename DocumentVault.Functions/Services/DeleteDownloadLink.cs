using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace DocumentVault.Functions.Services
{
    public class DeleteDownloadLink : IDeleteDownloadLink
    {
        private readonly Container _container;
        public DeleteDownloadLink(CosmosClient cosmosClient, IConfiguration configuration)
        {
            var databaseName = configuration["CosmosDB:DatabaseName"];
            var containerName = configuration["CosmosDB:ContainerName"];
            _container = cosmosClient.GetContainer(databaseName, containerName);
        }
        public async Task<IEnumerable<DownloadLink>> GetExpiredLinksAsync()
        {
            var query = "SELECT * FROM c WHERE c.partitionKey = 'links' AND c.expiresAt < @now";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@now", DateTime.UtcNow);

            var queryIterator = _container.GetItemQueryIterator<DownloadLink>(queryDefinition);
            var expiredLinks = new List<DownloadLink>();
             
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                expiredLinks.AddRange(response);
            }

            return expiredLinks;


        }
        public async Task<int> DeleteAllExpiredLinksAsync()
        {
            var expiredLinks = await GetExpiredLinksAsync();
            int deletedCount = 0;

            foreach (var link in expiredLinks)
            {
                try
                {
                    await _container.DeleteItemAsync<DownloadLink>(link.id, new PartitionKey("links"));
                    deletedCount++;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                 
                    continue;
                }
            }

            return deletedCount;
        }

       
    }
}
