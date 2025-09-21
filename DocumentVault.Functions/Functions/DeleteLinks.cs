using DocumentVault.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DocumentVault.Functions.Functions
{
    public class DeleteLinks
    {
        private readonly IDeleteDownloadLink _deleteDownloadLink;
        private readonly ILogger<DeleteLinks> _logger;

        public DeleteLinks(IDeleteDownloadLink deleteDownloadLink, ILogger<DeleteLinks> logger)
        {
            _deleteDownloadLink = deleteDownloadLink;
            _logger = logger;
        }

        [Function("DeleteLinks")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("DeleteLinks function started processing request at {DateTime}", DateTime.UtcNow);

            try
            {
                var deletedCount = await _deleteDownloadLink.DeleteAllExpiredLinksAsync();

                _logger.LogInformation("Successfully deleted {DeletedCount} expired links", deletedCount);

                return new OkObjectResult(new
                {
                    message = $"Successfully deleted {deletedCount} expired links",
                    deletedCount = deletedCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting expired links");

                return new ObjectResult(new
                {
                    error = "An error occurred while deleting expired links",
                    timestamp = DateTime.UtcNow
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}