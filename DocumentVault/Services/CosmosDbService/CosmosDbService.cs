using DocumentVault.Models;
using Microsoft.Azure.Cosmos;

namespace DocumentVault.Services.CosmosDbService
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _documentsContainer;
        private readonly Container _linksContainer;

        public CosmosDbService(CosmosClient cosmosClient, IConfiguration configuration)
        {
            var databaseName = configuration["Azure:CosmosDB:DatabaseName"];
            var containerName = configuration["Azure:CosmosDB:ContainerName"];
            _documentsContainer = cosmosClient.GetContainer(databaseName, containerName);
            _linksContainer = cosmosClient.GetContainer(databaseName, "links");
        }

        public async Task<Document> CreateDocumentAsync(Document document)
        {
            var response = await _documentsContainer.CreateItemAsync(document, new PartitionKey(document.partitionKey));
            return response.Resource;
        }

        public async Task<DownloadLink> CreateDownloadLinkAsync(DownloadLink downloadLink)
        {
            var response = await _linksContainer.CreateItemAsync(downloadLink, new PartitionKey(downloadLink.partitionKey));
            return response.Resource;
        }

        public async Task<bool> DeleteDocumentAsync(string id)
        {
            try
            {
                await _documentsContainer.DeleteItemAsync<Document>(id, new PartitionKey("documents"));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDownloadLinkAsync(string linkId)
        {
            try
            {
                await _linksContainer.DeleteItemAsync<DownloadLink>(linkId, new PartitionKey("links"));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
        {
            var query = "SELECT * FROM c WHERE c.partitionKey = 'documents'";
            var queryDefinition = new QueryDefinition(query);
            var queryIterator = _documentsContainer.GetItemQueryIterator<Document>(queryDefinition);

            var documents = new List<Document>();
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                documents.AddRange(response);
            }

            return documents;
        }

        public async Task<Document?> GetDocumentByIdAsync(string id)
        {
            try
            {
                var response = await _documentsContainer.ReadItemAsync<Document>(id, new PartitionKey("documents"));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<Document>> GetDocumentsByTagsAsync(List<string> tags)
        {
            if (!tags.Any())
                return new List<Document>();

            var conditions = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                conditions.Add($"ARRAY_CONTAINS(c.Tags, @tag{i})");
            }

            var query = $@"SELECT c.id, c.partitionKey, c.FileName, c.BlobName, c.ContentType, c.FileSize, c.Tags, c.UploadDate 
                   FROM c 
                   WHERE c.partitionKey = @partitionKey 
                   AND ({string.Join(" OR ", conditions)})";

            var queryDefinition = new QueryDefinition(query);
            queryDefinition.WithParameter("@partitionKey", "documents");

            for (int i = 0; i < tags.Count; i++)
            {
                queryDefinition.WithParameter($"@tag{i}", tags[i]);
            }

            var queryIterator = _documentsContainer.GetItemQueryIterator<Document>(queryDefinition);
            var documents = new List<Document>();

            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                documents.AddRange(response);
            }

            return documents;
        }

        public async Task<DownloadLink?> GetDownloadLinkAsync(string linkId)
        {
            try
            {
                var response = await _linksContainer.ReadItemAsync<DownloadLink>(linkId, new PartitionKey("links"));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<DownloadLink>> GetExpiredLinksAsync()
        {
            var query = "SELECT * FROM c WHERE c.partitionKey = 'links' AND c.expiresAt < @now";
            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@now", DateTime.UtcNow);

            var queryIterator = _linksContainer.GetItemQueryIterator<DownloadLink>(queryDefinition);

            var links = new List<DownloadLink>();
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                links.AddRange(response);
            }

            return links;
        }

        public async Task<bool> UpdateDownloadLinkAsync(DownloadLink downloadLink)
        {
            try
            {
                await _linksContainer.ReplaceItemAsync(downloadLink, downloadLink.id, new PartitionKey(downloadLink.partitionKey));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<IEnumerable<DownloadLink>> GetAllDownloadLinksAsync()
        {
            var query = "SELECT * FROM c WHERE c.partitionKey = 'links' ORDER BY c.createdAt DESC";
            var queryDefinition = new QueryDefinition(query);

            var queryIterator = _linksContainer.GetItemQueryIterator<DownloadLink>(queryDefinition);
            var links = new List<DownloadLink>();

            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                links.AddRange(response);
            }

            return links;
        }
    }
}